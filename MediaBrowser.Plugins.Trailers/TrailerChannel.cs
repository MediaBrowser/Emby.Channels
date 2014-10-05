using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers
{
    public class TrailerChannel : IChannel, IIndexableChannel, ISupportsLatestMedia, IHasCacheKey
    {
        public static TrailerChannel Instance;
        private readonly IJsonSerializer _json;

        public TrailerChannel(IJsonSerializer json)
        {
            _json = json;
            Instance = this;
        }

        public string DataVersion
        {
            get
            {
                return "61";
            }
        }

        public string GetCacheKey(string userId)
        {
            return Plugin.Instance.Configuration.EnableMovieArchive + "-" +
                   Plugin.Instance.Configuration.ForceDownloadListings;
        }

        public string Description
        {
            get { return string.Empty; }
        }

        private DateTime GetDateCreatedForSorting(ChannelItemInfo item)
        {
            var date = item.DateCreated;

            if (date.HasValue)
            {
                // Strip out the time portion in case they were all added at the same time
                // This will allow premiere date to take over
                return date.Value.Date;
            }

            return DateTime.MinValue;
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(query.FolderId))
            {
                return GetTopCategories();
            }

            var idParts = query.FolderId.Split('|');
            var contentType = (ChannelMediaContentType)Enum.Parse(typeof(ChannelMediaContentType), idParts[0], true);

            if (idParts.Length == 1)
            {
                return GetCategories(contentType);
            }

            var extraType = (ExtraType)Enum.Parse(typeof(ExtraType), idParts[1], true);
            var trailerType = (TrailerType)Enum.Parse(typeof(TrailerType), idParts[2], true);

            var items = await GetChannelItems(contentType, extraType, trailerType, cancellationToken).ConfigureAwait(false);

            if (query.SortBy.HasValue)
            {
                if (query.SortDescending)
                {
                    switch (query.SortBy.Value)
                    {
                        case ChannelItemSortField.Runtime:
                            items = items.OrderByDescending(i => i.RunTimeTicks ?? 0);
                            break;
                        case ChannelItemSortField.PremiereDate:
                            items = items.OrderByDescending(i => i.PremiereDate ?? DateTime.MinValue);
                            break;
                        case ChannelItemSortField.DateCreated:
                            items = items.OrderByDescending(GetDateCreatedForSorting)
                                .ThenByDescending(i => i.PremiereDate ?? DateTime.MinValue);
                            break;
                        case ChannelItemSortField.CommunityRating:
                            items = items.OrderByDescending(i => i.CommunityRating ?? 0);
                            break;
                        default:
                            items = items.OrderByDescending(i => i.Name);
                            break;
                    }
                }
                else
                {
                    switch (query.SortBy.Value)
                    {
                        case ChannelItemSortField.Runtime:
                            items = items.OrderBy(i => i.RunTimeTicks ?? 0);
                            break;
                        case ChannelItemSortField.PremiereDate:
                            items = items.OrderBy(i => i.PremiereDate ?? DateTime.MinValue);
                            break;
                        case ChannelItemSortField.DateCreated:
                            items = items.OrderBy(GetDateCreatedForSorting)
                                .ThenByDescending(i => i.PremiereDate ?? DateTime.MinValue);
                            break;
                        case ChannelItemSortField.CommunityRating:
                            items = items.OrderBy(i => i.CommunityRating ?? 0);
                            break;
                        default:
                            items = items.OrderBy(i => i.Name);
                            break;
                    }
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetChannelItems(ChannelMediaContentType contentType, ExtraType extraType, TrailerType trailerType, CancellationToken cancellationToken)
        {
            var items = await GetAllItems(false, cancellationToken).ConfigureAwait(false);

            return items
                .Where(i => i.ContentType == contentType && i.ExtraType == extraType && i.TrailerTypes.Contains(trailerType));
        }

        private IEnumerable<ChannelItemInfo> RemoveDuplicates(IEnumerable<ChannelItemInfo> items)
        {
            items = RemoveDuplicatesById(items);
            items = RemoveDuplicatesByName(items);

            return items;
        }

        private IEnumerable<ChannelItemInfo> RemoveDuplicatesById(IEnumerable<ChannelItemInfo> items)
        {
            var dictionary = new Dictionary<string, ChannelItemInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                var key = item.Id;

                if (dictionary.ContainsKey(key))
                {
                    var current = dictionary[key];

                    var types = current.TrailerTypes.ToList();
                    types.AddRange(item.TrailerTypes);
                    item.TrailerTypes = types.Distinct().ToList();
                }

                dictionary[key] = item;
            }

            return dictionary.Values;
        }

        private IEnumerable<ChannelItemInfo> RemoveDuplicatesByName(IEnumerable<ChannelItemInfo> items)
        {
            var dictionary = new Dictionary<string, ChannelItemInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                var key = item.Name;

                if (dictionary.ContainsKey(key))
                {
                    var current = dictionary[key];

                    var types = current.TrailerTypes.ToList();
                    types.AddRange(item.TrailerTypes);
                    item.TrailerTypes = types.Distinct().ToList();

                    var sources = current.MediaSources.ToList();
                    sources.AddRange(item.MediaSources.Where(i => !sources.Any(s => string.Equals(s.Path, i.Path, StringComparison.OrdinalIgnoreCase))));
                    item.MediaSources = sources;
                }

                dictionary[key] = item;
            }

            return dictionary.Values;
        }

        private async Task<IEnumerable<ChannelItemInfo>> GetChannelItems(IEnumerable<IExtraProvider> providers, CancellationToken cancellationToken)
        {
            var tasks = providers.Select(async i =>
            {
                try
                {
                    var items = await i.GetChannelItems(cancellationToken).ConfigureAwait(false);

                    items = items.Where(t => !string.IsNullOrWhiteSpace(t.Name) && t.MediaSources.Count > 0);

                    return items;
                }
                catch (Exception ex)
                {
                    return new List<ChannelItemInfo>();
                }
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return results.SelectMany(i => i);
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetAllItems(bool direct, CancellationToken cancellationToken)
        {
            if (direct || Plugin.Instance.Configuration.ForceDownloadListings)
            {
                var items = await GetChannelItems(EntryPoint.Instance.Providers, cancellationToken).ConfigureAwait(false);

                items = RemoveDuplicates(items);

                if (!Plugin.Instance.Configuration.EnableMovieArchive)
                {
                    items = items.Where(i => i.TrailerTypes.Count > 1 || !i.TrailerTypes.Contains(TrailerType.Archive));
                }

                return items;
            }

            var json = await EntryPoint.Instance.GetAndCacheResponse("https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Providers/listings.txt?v=" + DataVersion,
                        TimeSpan.FromDays(3), cancellationToken);

            return _json.DeserializeFromString<List<ChannelItemInfo>>(json);
        }

        public ChannelItemResult GetTopCategories()
        {
            var list = new List<ChannelItemInfo>();

            //list.Add(new ChannelItemInfo
            //{
            //    FolderType = ChannelFolderType.Container,
            //    Name = "Movies",
            //    Type = ChannelItemType.Folder,
            //    MediaType = ChannelMediaType.Video,
            //    Id = ChannelMediaContentType.MovieExtra.ToString().ToLower(),
            //    ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/thumb.jpg"
            //});

            list.Add(new ChannelItemInfo
            {
                FolderType = ChannelFolderType.Container,
                Name = "New and Upcoming in Theaters",
                Type = ChannelItemType.Folder,
                MediaType = ChannelMediaType.Video,
                Id = ChannelMediaContentType.MovieExtra.ToString().ToLower() + "|" + ExtraType.Trailer + "|" + TrailerType.ComingSoonToTheaters,

                ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/thumb.jpg"
            });

            list.Add(new ChannelItemInfo
            {
                FolderType = ChannelFolderType.Container,
                Name = "New and Upcoming Movies on Dvd & Blu-ray",
                Type = ChannelItemType.Folder,
                MediaType = ChannelMediaType.Video,
                Id = ChannelMediaContentType.MovieExtra.ToString().ToLower() + "|" + ExtraType.Trailer + "|" + TrailerType.ComingSoonToDvd,

                ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/bluray.jpg"
            });

            list.Add(new ChannelItemInfo
            {
                FolderType = ChannelFolderType.Container,
                Name = "New and Upcoming Movies on Netflix",
                Type = ChannelItemType.Folder,
                MediaType = ChannelMediaType.Video,
                Id = ChannelMediaContentType.MovieExtra.ToString().ToLower() + "|" + ExtraType.Trailer + "|" + TrailerType.ComingSoonToStreaming,

                ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/netflix.png"
            });

            if (Plugin.Instance.Configuration.EnableMovieArchive)
            {
                list.Add(new ChannelItemInfo
                {
                    FolderType = ChannelFolderType.Container,
                    Name = "Movie Trailer Archive",
                    Type = ChannelItemType.Folder,
                    MediaType = ChannelMediaType.Video,
                    Id = ChannelMediaContentType.MovieExtra.ToString().ToLower() + "|" + ExtraType.Trailer + "|" + TrailerType.Archive,

                    ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/reel.jpg"
                });
            }

            return new ChannelItemResult
            {
                Items = list,
                TotalRecordCount = list.Count
            };
        }

        public ChannelItemResult GetCategories(ChannelMediaContentType contentType)
        {
            var list = new List<ChannelItemInfo>();

            //list.Add(new ChannelItemInfo
            //{
            //    FolderType = ChannelFolderType.Container,
            //    Name = "New and coming soon to theaters",
            //    Type = ChannelItemType.Folder,
            //    MediaType = ChannelMediaType.Video,
            //    Id = contentType.ToString().ToLower() + "|" + "TrailerComingSoonToTheaters",

            //    ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/thumb.jpg"
            //});

            //list.Add(new ChannelItemInfo
            //{
            //    FolderType = ChannelFolderType.Container,
            //    Name = "New and coming soon to Dvd",
            //    Type = ChannelItemType.Folder,
            //    MediaType = ChannelMediaType.Video,
            //    Id = contentType.ToString().ToLower() + "|" + "TrailerComingSoonToDvd",

            //    ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/bluray.jpg"
            //});

            //list.Add(new ChannelItemInfo
            //{
            //    FolderType = ChannelFolderType.Container,
            //    Name = "Archive",
            //    Type = ChannelItemType.Folder,
            //    MediaType = ChannelMediaType.Video,
            //    Id = contentType.ToString().ToLower() + "|" + "TrailerArchive",

            //    ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/reel.jpg"
            //});

            return new ChannelItemResult
            {
                Items = list,
                TotalRecordCount = list.Count
            };
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Primary:
                case ImageType.Thumb:
                    {
                        var path = GetType().Namespace + ".Images." + type.ToString().ToLower() + ".jpg";

                        return Task.FromResult(new DynamicImageResponse
                        {
                            Format = ImageFormat.Jpg,
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

        public string HomePageUrl
        {
            get { return "http://mediabrowser.tv"; }
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public string Name
        {
            get { return "Trailers"; }
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                 {
                     ChannelMediaContentType.MovieExtra
                 },

                MediaTypes = new List<ChannelMediaType>
                  {
                       ChannelMediaType.Video
                  },

                SupportsSortOrderToggle = true,

                DefaultSortFields = new List<ChannelItemSortField>
                   {
                        ChannelItemSortField.CommunityRating,
                        ChannelItemSortField.Name,
                        ChannelItemSortField.DateCreated,
                        ChannelItemSortField.PremiereDate,
                        ChannelItemSortField.Runtime
                   },

                AutoRefreshLevels = 3,
                DailyDownloadLimit = 10
            };
        }

        public async Task<ChannelItemResult> GetAllMedia(InternalAllChannelMediaQuery query, CancellationToken cancellationToken)
        {
            var items = await GetAllItems(false, cancellationToken).ConfigureAwait(false);

            if (query.ContentTypes.Length > 0)
            {
                items = items.Where(i => query.ContentTypes.Contains(i.ContentType));
            }
            if (query.ExtraTypes.Length > 0)
            {
                items = items.Where(i => query.ExtraTypes.Contains(i.ExtraType));
            }
            if (query.TrailerTypes.Length > 0)
            {
                items = items.Where(i => i.TrailerTypes.Any(t => query.TrailerTypes.Contains(t)));
            }

            var list = items.ToList();

            return new ChannelItemResult
            {
                Items = list,
                TotalRecordCount = list.Count
            };
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetLatestMedia(ChannelLatestMediaSearch request, CancellationToken cancellationToken)
        {
            var result = await GetChannelItems(new InternalChannelItemQuery
            {
                SortBy = ChannelItemSortField.DateCreated,
                SortDescending = true,
                UserId = request.UserId,
                FolderId = ChannelMediaContentType.MovieExtra.ToString().ToLower() + "|" + ExtraType.Trailer + "|" + TrailerType.ComingSoonToTheaters

            }, cancellationToken).ConfigureAwait(false);

            return result.Items;
        }
    }
}
