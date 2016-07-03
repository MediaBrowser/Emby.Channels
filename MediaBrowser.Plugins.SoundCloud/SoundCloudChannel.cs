using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.SoundCloud.ClientApi;
using MediaBrowser.Plugins.SoundCloud.ClientApi.Model;
using MediaBrowser.Plugins.SoundCloud.ExternalIds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.SoundCloud
{
    public class SoundCloudChannel : IChannel, ISupportsLatestMedia, IHasCacheKey
    {
        public const string ChannelName = "SoundCloud";

        private readonly ILogger _logger;
        public SoundCloudChannel(ILogManager logManager)
        {
            _logger = logManager.GetLogger(GetType().Name);
        }

        public string DataVersion
        {
            get
            {
                // Increment as needed to invalidate all caches
                return "35";
            }
        }

        public string Name
        {
            get { return ChannelName; }
        }

        public string Description
        {
            get { return "SoundCloud is the world’s leading social sound platform where anyone can create sounds and share them everywhere."; }
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Thumb:
                case ImageType.Primary:
                    {
                        var path = GetType().Namespace + ".Images." + type.ToString().ToLower() + ".png";

                        return Task.FromResult(new DynamicImageResponse
                        {
                            Format = ImageFormat.Png,
                            HasImage = true,
                            Stream = GetType().Assembly.GetManifestResourceStream(path)
                        });
                    }
                default:
                    throw new ArgumentException("Unsupported image type: " + type);
            }
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Thumb,
                ImageType.Primary
            };
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                 {
                     ChannelMediaContentType.Song,
                     ChannelMediaContentType.Podcast
                 },

                SupportsSortOrderToggle = true,
                DefaultSortFields = new List<ChannelItemSortField> { ChannelItemSortField.Name, ChannelItemSortField.PremiereDate, ChannelItemSortField.DateCreated, ChannelItemSortField.CommunityRating, ChannelItemSortField.Runtime },

                MediaTypes = new List<ChannelMediaType>
                  {
                       ChannelMediaType.Audio
                  },

                MaxPageSize = 50,

                AutoRefreshLevels = 3
            };
        }

        public string HomePageUrl
        {
            get { return "http://www.soundcloud.com/"; }
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public string GetCacheKey(string userId)
        {
            return Plugin.Instance.Configuration.Username;
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            if (query.FolderId == null)
            {
                query.SortBy = null;
                return await GetRootFolders(cancellationToken);
            }

            var catSplit = query.FolderId.Split('_');


            if (catSplit[0] == "myDashboard")
            {
                return await GetDashboard(query, cancellationToken).ConfigureAwait(false);
            }

            if (catSplit[0] == "user")
            {
                query.SortBy = null;
                var userId = Convert.ToInt32(catSplit[1]);
                return await GetUserRoot(userId, cancellationToken).ConfigureAwait(false);
            }

            if (catSplit[0] == "followings")
            {
                var userId = Convert.ToInt32(catSplit[1]);
                return await GetFollowings(userId, query, cancellationToken).ConfigureAwait(false);
            }

            if (catSplit[0] == "followers")
            {
                var userId = Convert.ToInt32(catSplit[1]);
                return await GetFollowers(userId, query, cancellationToken).ConfigureAwait(false);
            }

            if (catSplit[0] == "userplaylists")
            {
                var userId = Convert.ToInt32(catSplit[1]);
                return await GetPlayLists(userId, query, cancellationToken).ConfigureAwait(false);
            }

            if (catSplit[0] == "usertracks")
            {
                var userId = Convert.ToInt32(catSplit[1]);
                return await GetUserTracks(userId, query, cancellationToken).ConfigureAwait(false);
            }

            if (catSplit[0] == "playlist")
            {
                var playlistId = Convert.ToInt32(catSplit[1]);
                return await GetPlayListTracks(playlistId, query, cancellationToken).ConfigureAwait(false);
            }

            if (catSplit[0] == "favorites")
            {
                var playlistId = Convert.ToInt32(catSplit[1]);
                return await GetFavorites(playlistId, query, cancellationToken).ConfigureAwait(false);
            }

            if (catSplit[0] == "cat")
            {
                return await GetLatestTracks(query, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        private async Task<ChannelItemResult> GetRootFolders(CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>();

            if (Plugin.Instance.IsAuthenticated)
            {
                try
                {
                    var user = await Plugin.Instance.Client.Api.GetMe(cancellationToken);

                    items.Add(this.CreatePersonInfoFromUser(user));

                    items.Add(new ChannelItemInfo
                    {
                        FolderType = ChannelFolderType.Container,
                        MediaType = ChannelMediaType.Audio,
                        Name = user.username + " - Dashboard",
                        Id = "myDashboard",
                        Type = ChannelItemType.Folder,
                        ImageUrl = this.FixArtworkUrl(user.avatar_url)
                    });

                    if (user.track_count > 0)
                    {
                        items.Add(new ChannelItemInfo
                        {
                            FolderType = ChannelFolderType.Container,
                            MediaType = ChannelMediaType.Audio,
                            Name = string.Format("My Tracks [{0}]", user.track_count),
                            Id = string.Format("usertracks_{0}", user.id),
                            Type = ChannelItemType.Folder,
                            ImageUrl = this.FixArtworkUrl(user.avatar_url)
                        });
                    }

                    if (user.playlist_count > 0)
                    {
                        items.Add(new ChannelItemInfo
                        {
                            FolderType = ChannelFolderType.Container,
                            MediaType = ChannelMediaType.Audio,
                            Name = string.Format("My Playlists [{0}]", user.playlist_count),
                            Id = string.Format("userplaylists_{0}", user.id),
                            Type = ChannelItemType.Folder,
                            ImageUrl = this.FixArtworkUrl(user.avatar_url)
                        });
                    }

                    if (user.followings_count > 0)
                    {
                        items.Add(new ChannelItemInfo
                        {
                            FolderType = ChannelFolderType.Container,
                            MediaType = ChannelMediaType.Audio,
                            Name = string.Format("I'm Following [{0}]", user.followings_count),
                            Id = string.Format("followings_{0}", user.id),
                            Type = ChannelItemType.Folder,
                            ImageUrl = this.FixArtworkUrl(user.avatar_url)
                        });
                    }

                    if (user.followers_count > 0)
                    {
                        items.Add(new ChannelItemInfo
                        {
                            FolderType = ChannelFolderType.Container,
                            MediaType = ChannelMediaType.Audio,
                            Name = string.Format("My Followers [{0}]", user.followers_count),
                            Id = string.Format("followers_{0}", user.id),
                            Type = ChannelItemType.Folder,
                            ImageUrl = this.FixArtworkUrl(user.avatar_url)
                        });
                    }

                    if (user.public_favorites_count > 0)
                    {
                        items.Add(new ChannelItemInfo
                        {
                            FolderType = ChannelFolderType.Container,
                            MediaType = ChannelMediaType.Audio,
                            Name = string.Format("My Favorites [{0}]", user.public_favorites_count),
                            Id = string.Format("favorites_{0}", user.id),
                            Type = ChannelItemType.Folder,
                            ImageUrl = this.FixArtworkUrl(user.avatar_url)
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error loading SoundCloud user data", ex);
                }
            }
            else
            {
                items.Add(new ChannelItemInfo
                {
                    Name = "Latest Items",
                    Id = "cat_hot",
                    Type = ChannelItemType.Folder
                });

                items.Add(new ChannelItemInfo
                {
                    Name = "Please Login for Full Experience",
                    Id = "cat_latest",
                    Type = ChannelItemType.Folder,
                    ImageUrl = ImageLinks.ImageLogin
                });
            }

            return new ChannelItemResult
            {
                Items = items.ToList(),
                TotalRecordCount = items.Count
            };
        }

        private async Task<ChannelItemResult> GetUserRoot(int userId, CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>();

            try
            {
                var user = await Plugin.Instance.Client.Api.GetUser(userId, cancellationToken);

                items.Add(this.CreatePersonInfoFromUser(user));

                if (user.track_count > 0)
                {
                    items.Add(new ChannelItemInfo
                    {
                        FolderType = ChannelFolderType.Container,
                        MediaType = ChannelMediaType.Audio,
                        Name = string.Format("{0}: Tracks [{1}]", user.username, user.track_count),
                        Id = string.Format("usertracks_{0}", user.id),
                        Type = ChannelItemType.Folder,
                        ImageUrl = this.FixArtworkUrl(user.avatar_url)
                    });
                }

                if (user.playlist_count > 0)
                {
                    items.Add(new ChannelItemInfo
                    {
                        FolderType = ChannelFolderType.Container,
                        MediaType = ChannelMediaType.Audio,
                        Name = string.Format("{0}: Playlists [{1}]", user.username, user.playlist_count),
                        Id = string.Format("userplaylists_{0}", user.id),
                        Type = ChannelItemType.Folder,
                        ImageUrl = this.FixArtworkUrl(user.avatar_url)
                    });
                }

                if (user.followings_count > 0)
                {
                    items.Add(new ChannelItemInfo
                    {
                        FolderType = ChannelFolderType.Container,
                        MediaType = ChannelMediaType.Audio,
                        Name = string.Format("{0}: Following [{1}]", user.username, user.followings_count),
                        Id = string.Format("followings_{0}", user.id),
                        Type = ChannelItemType.Folder,
                        ImageUrl = this.FixArtworkUrl(user.avatar_url)
                    });
                }

                if (user.followers_count > 0)
                {
                    items.Add(new ChannelItemInfo
                    {
                        FolderType = ChannelFolderType.Container,
                        MediaType = ChannelMediaType.Audio,
                        Name = string.Format("{0}: Followers [{1}]", user.username, user.followers_count),
                        Id = string.Format("followers_{0}", user.id),
                        Type = ChannelItemType.Folder,
                        ImageUrl = this.FixArtworkUrl(user.avatar_url)
                    });
                }

                if (user.public_favorites_count > 0)
                {
                    items.Add(new ChannelItemInfo
                    {
                        FolderType = ChannelFolderType.Container,
                        MediaType = ChannelMediaType.Audio,
                        Name = string.Format("{0}: Favorites [{1}]", user.username, user.public_favorites_count),
                        Id = string.Format("favorites_{0}", user.id),
                        Type = ChannelItemType.Folder,
                        ImageUrl = this.FixArtworkUrl(user.avatar_url)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error loading SoundCloud user data", ex);
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        private async Task<ChannelItemResult> GetUserTracks(int userId, InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var user = await Plugin.Instance.Client.Api.GetUser(userId, cancellationToken);

            query.Limit = 50;

            var trackResult = await Plugin.Instance.Client.Api.GetUserTracks(userId, cancellationToken, this.GetPagingInfo(query));

            var items = trackResult.collection.Select(this.CreateInfoFromTrack).ToList();

            return new ChannelItemResult
            {
                Items = items,
                TotalRecordCount = user.track_count
            };
        }

        private async Task<ChannelItemResult> GetLatestTracks(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var offset = query.StartIndex.GetValueOrDefault();

            var trackResult = await Plugin.Instance.Client.Api.GetLatestTracks(cancellationToken, this.GetPagingInfo(query));

            var items = trackResult.collection.Select(this.CreateInfoFromTrack).ToList();

            return new ChannelItemResult
            {
                Items = items,
                TotalRecordCount = items.Count() + offset + 1
            };
        }

        private async Task<ChannelItemResult> GetPlayLists(int userId, InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var user = await Plugin.Instance.Client.Api.GetUser(userId, cancellationToken);

            var result = await Plugin.Instance.Client.Api.GetPlaylists(userId, cancellationToken, this.GetPagingInfo(query));
            var playlists = result;

            var itemInfos = playlists.Select(this.CreateInfoFromPlaylist);

            return new ChannelItemResult
            {
                Items = itemInfos.ToList(),
                TotalRecordCount = user.playlist_count
            };
        }

        private async Task<ChannelItemResult> GetPlayListTracks(int playlistID, InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var result = await Plugin.Instance.Client.Api.GetPlaylist(playlistID, cancellationToken);

            var items = result.tracks.Select(this.CreateInfoFromTrack).ToList();
            this.SetIndexNumbers(items);

            return new ChannelItemResult
            {
                Items = items,
                TotalRecordCount = items.Count()
            };
        }

        private void SetIndexNumbers(IList<ChannelItemInfo> items)
        {
            int index = 1;

            foreach (var item in items)
            {
                item.IndexNumber = index;
                index++;
            }
        }

        private PagingInfo GetPagingInfo(InternalChannelItemQuery query)
        {
            int page = 0;

            if (query.StartIndex.HasValue && query.Limit.HasValue)
            {
                page = (query.StartIndex.Value / query.Limit.Value) % query.Limit.Value;
            }

            return new PagingInfo(query.Limit ?? this.GetChannelFeatures().MaxPageSize.Value, page);
        }

        private async Task<ChannelItemResult> GetDashboard(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            query.SortBy = ChannelItemSortField.DateCreated;
            query.SortDescending = true;

            var offset = query.StartIndex.GetValueOrDefault();

            var result = await Plugin.Instance.Client.Api.GetActivities(cancellationToken, this.GetPagingInfo(query));

            if (result.collection != null)
            {
                var items = result.collection.Where(e => e.origin != null && (e.IsTrack() || e.IsPlaylist())).Select(i =>
                    i.IsTrack() ? this.CreateInfoFromOriginTrack(i.origin, i.created_at) : this.CreateInfoFromOriginPlaylist(i.origin, i.created_at)
                );

                return new ChannelItemResult
                {
                    Items = items.ToList(),
                    TotalRecordCount = items.Count() + offset + 1
                };
            }

            return new ChannelItemResult();
        }

        private async Task<ChannelItemResult> GetFollowings(int userId, InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var user = await Plugin.Instance.Client.Api.GetUser(userId, cancellationToken);

            var result = await Plugin.Instance.Client.Api.GetFollowings(userId, cancellationToken, this.GetPagingInfo(query));

            if (result.collection != null)
            {
                var users = result.collection;

                var itemInfos = users.Select(this.CreateFolderInfoFromUser);

                return new ChannelItemResult
                {
                    Items = itemInfos.ToList(),
                    TotalRecordCount = user.followings_count
                };
            }

            return new ChannelItemResult();
        }

        private async Task<ChannelItemResult> GetFollowers(int userId, InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var user = await Plugin.Instance.Client.Api.GetUser(userId, cancellationToken);

            var result = await Plugin.Instance.Client.Api.GetFollowers(userId, cancellationToken, this.GetPagingInfo(query));

            if (result.collection != null)
            {
                var users = result.collection;

                var itemInfos = users.Select(this.CreateFolderInfoFromUser);

                return new ChannelItemResult
                {
                    Items = itemInfos.ToList(),
                    TotalRecordCount = user.followers_count
                };
            }

            return new ChannelItemResult();
        }

        private async Task<ChannelItemResult> GetFavorites(int userId, InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var user = await Plugin.Instance.Client.Api.GetUser(userId, cancellationToken);

            var result = await Plugin.Instance.Client.Api.GetFavorites(userId, cancellationToken, this.GetPagingInfo(query));

            if (result.collection != null)
            {
                var itemInfos = result.collection.Select(this.CreateInfoFromTrack);

                return new ChannelItemResult
                {
                    Items = itemInfos.ToList(),
                    TotalRecordCount = user.public_favorites_count
                };
            }

            return new ChannelItemResult();
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetLatestMedia(ChannelLatestMediaSearch request, CancellationToken cancellationToken)
        {
            if (Plugin.Instance.IsAuthenticated)
            {
                var result = await Plugin.Instance.Client.Api.GetActivities(cancellationToken, new PagingInfo(10, 0));

                if (result.collection != null)
                {
                    var items = result.collection.Where(e => e.IsTrack() || e.IsPlaylist()).Select(i =>
                        i.IsTrack() ? this.CreateInfoFromOriginTrack(i.origin, i.created_at) : this.CreateInfoFromOriginPlaylist(i.origin, i.created_at)
                    );

                    return items.ToList();
                }
            }

            return new List<ChannelItemInfo>();
        }

        private ChannelItemInfo CreateInfoFromTrack(Track track)
        {
            var premiereDate = DateTime.Parse(track.created_at);

            if (track.release_year.HasValue && track.release_month.HasValue)
            {
                var day = track.release_day.HasValue ? track.release_day.Value : 1;
                premiereDate = new DateTime(track.release_year.Value, track.release_month.Value, day);
            }

            return new ChannelItemInfo
                {
                    CommunityRating = Convert.ToSingle(track.playback_count),
                    ContentType = ChannelMediaContentType.Song,
                    PremiereDate = premiereDate,
                    DateCreated = DateTime.Parse(track.created_at),
                    Genres = new List<string> { track.genre },
                    Id = track.id.ToString(),
                    ImageUrl = this.FixArtworkUrl(track.artwork_url),
                    IsInfiniteStream = false,
                    MediaType = ChannelMediaType.Audio,
                    Name = track.title,
                    Type = ChannelItemType.Media,
                    Overview = track.description,
                    RunTimeTicks = TimeSpan.FromMilliseconds(track.duration).Ticks,
                    Tags = this.ParseTagList(track.tag_list),
                    HomePageUrl = track.user.permalink_url,
                    Artists = new List<string> { track.user.username },
                    ProviderIds = this.CreateProvIdsTrack(track.id, track.permalink_url, track.purchase_url, track.download_url),

                    MediaSources = new List<ChannelMediaInfo>
                    {
                        new ChannelMediaInfo
                        {
                            Path = AppendClientId(track.stream_url)
                        }
                    }
                };
        }

        private ChannelItemInfo CreateInfoFromOriginTrack(Origin origin, string created)
        {
            var premiereDate = DateTime.Parse(created);

            if (origin.release_year.HasValue && origin.release_month.HasValue)
            {
                var day = origin.release_day.HasValue ? origin.release_day.Value : 1;
                premiereDate = new DateTime(origin.release_year.Value, origin.release_month.Value, day);
            }

            return new ChannelItemInfo
                {
                    CommunityRating = Convert.ToSingle(origin.likes_count),
                    ContentType = ChannelMediaContentType.Song,
                    PremiereDate = premiereDate,
                    DateCreated = DateTime.Parse(created),
                    Genres = new List<string> { origin.genre },
                    Id = origin.id.ToString(),
                    ImageUrl = this.FixArtworkUrl(origin.artwork_url),
                    IsInfiniteStream = false,
                    MediaType = ChannelMediaType.Audio,
                    Name = origin.title,
                    Type = ChannelItemType.Media,
                    Overview = origin.description,
                    RunTimeTicks = TimeSpan.FromMilliseconds(origin.duration).Ticks,
                    Tags = this.ParseTagList(origin.tag_list),
                    HomePageUrl = origin.user.permalink_url,
                    Artists = new List<string> { origin.user.username },
                    ProviderIds = this.CreateProvIdsTrack(origin.id, origin.permalink_url, origin.purchase_url, origin.download_url),

                    MediaSources = new List<ChannelMediaInfo>
                    {
                        new ChannelMediaInfo
                        {
                            Path = AppendClientId(origin.stream_url)
                        }
                    }
                };
        }

        private ChannelItemInfo CreateInfoFromPlaylist(Playlist playlist)
        {
            var premiereDate = DateTime.Parse(playlist.created_at);

            if (playlist.release_year.HasValue && playlist.release_month.HasValue)
            {
                var day = playlist.release_day.HasValue ? playlist.release_day.Value : 1;
                premiereDate = new DateTime(playlist.release_year.Value, playlist.release_month.Value, day);
            }

            var albumArtists = new List<string>();

            foreach (var track in playlist.tracks)
            {
                if (track.user != null && !string.IsNullOrEmpty(track.user.username) && !albumArtists.Contains(track.user.username))
                {
                    albumArtists.Add(track.user.username);
                }
            }

            return new ChannelItemInfo
                {
                    PremiereDate = premiereDate,
                    DateCreated = DateTime.Parse(playlist.created_at),
                    Id = string.Format("playlist_{0}", playlist.id),
                    ImageUrl = this.FixArtworkUrl(playlist.artwork_url, playlist.user.avatar_url),
                    Name = playlist.title,
                    Type = ChannelItemType.Folder,
                    FolderType = ChannelFolderType.MusicAlbum,
                    Overview = playlist.description,
                    ProviderIds = this.CreateProvIdsPlaylist(playlist.id, playlist.permalink_url, playlist.purchase_url),
                    Genres = this.CreateSingleGenreList(playlist.genre),
                    HomePageUrl = playlist.user.permalink_url,
                    Tags = this.ParseTagList(playlist.tag_list),
                    Artists = new List<string> { playlist.user.username },
                    AlbumArtists = albumArtists
                };
        }

        private ChannelItemInfo CreateInfoFromOriginPlaylist(Origin origin, string created)
        {
            var premiereDate = DateTime.Parse(created);

            if (origin.release_year.HasValue && origin.release_month.HasValue)
            {
                var day = origin.release_day.HasValue ? origin.release_day.Value : 1;
                premiereDate = new DateTime(origin.release_year.Value, origin.release_month.Value, day);
            }

            return new ChannelItemInfo
                {
                    CommunityRating = Convert.ToSingle(origin.likes_count),
                    PremiereDate = premiereDate,
                    DateCreated = DateTime.Parse(created),
                    Id = string.Format("playlist_{0}", origin.id),
                    ImageUrl = this.FixArtworkUrl(origin.artwork_url),
                    Name = origin.title,
                    Type = ChannelItemType.Folder,
                    FolderType = ChannelFolderType.MusicAlbum,
                    Overview = origin.description,
                    ProviderIds = this.CreateProvIdsPlaylist(origin.id, origin.permalink_url, origin.purchase_url),
                    Genres = this.CreateSingleGenreList(origin.genre),
                    HomePageUrl = origin.user.permalink_url,
                    Tags = this.ParseTagList(origin.tag_list),
                    Artists = new List<string> { origin.user.username }
                };
        }

        private ChannelItemInfo CreateFolderInfoFromUser(User user)
        {
            return new ChannelItemInfo
                {
                    CommunityRating = user.followers_count,
                    Id = string.Format("user_{0}", user.id),
                    ImageUrl = this.FixArtworkUrl(user.avatar_url),
                    MediaType = ChannelMediaType.Audio,
                    Name = user.username,
                    Type = ChannelItemType.Folder,
                    Overview = user.description,
                    HomePageUrl = user.website,

                    People = new List<MediaBrowser.Controller.Entities.PersonInfo>
                    {
                        new MediaBrowser.Controller.Entities.PersonInfo
                        {
                            ImageUrl = this.FixArtworkUrl(user.avatar_url),
                            Name = user.username,
                            Role = "Owner",
                            Type = "user",
                            ProviderIds = this.CreateProvIdsUser(user.id, user.permalink_url)
                        }
                    }
                };
        }

        private ChannelItemInfo CreatePersonInfoFromUser(User user)
        {
            var location = user.city ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(user.country))
            {
                if (location.Length > 0)
                {
                    location += ", ";
                }

                location += user.country;
            }

            var productionLocations = new List<string>();

            if (!string.IsNullOrWhiteSpace(location))
            {
                productionLocations.Add(location);
            }

            return new ChannelItemInfo
                {
                    CommunityRating = user.followers_count,
                    Type = ChannelItemType.Folder,
                    FolderType = ChannelFolderType.MusicArtist,
                    Id = string.Format("userinfo_{0}", user.id),
                    ImageUrl = this.FixArtworkUrl(user.avatar_url),
                    Name = user.username,
                    Overview = user.description,
                    ProviderIds = this.CreateProvIdsUser(user.id, user.permalink_url),
                    HomePageUrl = user.website,
                    Studios = productionLocations
                };
        }

        private string AppendClientId(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            return string.Format("{0}?client_id={1}", url, SoundCloudApi.ClientIdForTracks);
        }

        private string FixArtworkUrl(string url, string alternateUrl = null)
        {
            var resultUrl = url;

            if (string.IsNullOrWhiteSpace(resultUrl))
            {
                resultUrl = alternateUrl;
            }

            if (string.IsNullOrWhiteSpace(resultUrl))
            {
                return null;
            }

            return resultUrl.Replace("-large.jpg", "-t500x500.jpg");
        }

        private Dictionary<string, string> CreateProvIdsUser(int userId, string permalink)
        {
            var dic = new Dictionary<string, string>();

            dic.Add(new SoundCloudUserId().Key, userId.ToString());

            if (!string.IsNullOrWhiteSpace(permalink))
            {
                dic.Add(new SoundCloudUserLink().Key, permalink);
            }

            return dic;
        }

        private Dictionary<string, string> CreateProvIdsPlaylist(int playlistId, string permalink, string purchaseLink)
        {
            var dic = new Dictionary<string, string>();

            dic.Add(new SoundCloudPlaylistId().Key, playlistId.ToString());

            if (!string.IsNullOrWhiteSpace(permalink))
            {
                dic.Add(new SoundCloudPlaylistLink().Key, permalink);
            }

            if (!string.IsNullOrWhiteSpace(purchaseLink))
            {
                dic.Add(new SoundCloudPurchaseLink().Key, purchaseLink);
            }

            return dic;
        }

        private Dictionary<string, string> CreateProvIdsTrack(int trackId, string permalink, string purchaseLink, string downloadLink)
        {
            var dic = new Dictionary<string, string>();

            dic.Add(new SoundCloudTrackId().Key, trackId.ToString());

            if (!string.IsNullOrWhiteSpace(permalink))
            {
                dic.Add(new SoundCloudTrackLink().Key, permalink);
            }

            if (!string.IsNullOrWhiteSpace(purchaseLink))
            {
                dic.Add(new SoundCloudPurchaseLink().Key, purchaseLink);
            }

            if (!string.IsNullOrWhiteSpace(downloadLink))
            {
                dic.Add(new SoundCloudDownloadTrackLink().Key, downloadLink);
            }

            return dic;
        }

        private List<string> CreateSingleGenreList(string genre)
        {
            var list = new List<string>();

            if (!string.IsNullOrWhiteSpace(genre))
            {
                list.Add(genre.Trim());
            }

            return list;
        }

        private List<string> ParseTagList(string tagstring)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(tagstring))
                {
                    var result = tagstring.Split('"')
                                         .Select((element, index) => index % 2 == 0  // If even index
                                                               ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                                               : new string[] { element })  // Keep the entire item
                                         .SelectMany(e1 => e1)
                                         .Where(e2 => !string.IsNullOrWhiteSpace(e2))
                                         .Select(e3 => e3.Trim()).ToList();

                    return result;
                }
            }
            catch (Exception) { }

            return new List<string>();
        }
    }
}