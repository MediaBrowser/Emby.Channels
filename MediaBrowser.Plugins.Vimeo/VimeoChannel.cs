﻿using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.Vimeo.VimeoAPI.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Vimeo
{
    public class VimeoChannel : IChannel, IRequiresMediaInfoCallback, IHasCacheKey
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public VimeoChannel(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            _httpClient = httpClient;
            _logger = logManager.GetLogger(GetType().Name);
            _jsonSerializer = jsonSerializer;
        }

        public string DataVersion
        {
            get
            {
                // Increment as needed to invalidate all caches
                return "11";
            }
        }

        public string GetCacheKey(string userId)
        {
            var vals = new List<string>();

            vals.Add(RegistrationInfo.Instance.IsRegistered.ToString());
            vals.Add(Plugin.Instance.Configuration.Username ?? string.Empty);

            return string.Join("-", vals.ToArray());
        }

        public async Task<IEnumerable<ChannelItemInfo>> Search(ChannelSearchInfo searchInfo, Controller.Entities.User user, CancellationToken cancellationToken)
        {
            var downloader = new VimeoListingDownloader(_logger, _jsonSerializer, _httpClient);
            var search = await downloader.GetSearchVimeoList(searchInfo.SearchTerm, cancellationToken);

            return search.Select(i => new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.Clip,
                ImageUrl = i.thumbnails[0].Url,
                IsInfiniteStream = false,
                MediaType = ChannelMediaType.Video,
                Name = i.title,
                Overview = i.description,
                Type = ChannelItemType.Media,
                Id = i.urls[0].Value.GetMD5().ToString("N"),

                MediaSources = new List<ChannelMediaInfo>
                {
                    new ChannelMediaInfo
                    {
                         Path = i.urls[0].Value,
                         Height = i.height,
                         Width = i.width
                    }
                }
            });
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            if (!RegistrationInfo.Instance.IsRegistered)
            {
                var list = new List<ChannelItemInfo>
                {
                    new ChannelItemInfo
                    {
                         Id = "notregistered",
                         Name = "Supporter membership required",
                         Type = ChannelItemType.Folder
                    }
                };

                return new ChannelItemResult
                {
                    Items = list,
                    TotalRecordCount = list.Count
                };
            }

            if (string.Equals(query.FolderId, "notregistered", StringComparison.OrdinalIgnoreCase))
            {
                return new ChannelItemResult();
            }

            if (string.IsNullOrEmpty(query.FolderId))
            {
                return await GetCategories(query, cancellationToken).ConfigureAwait(false);
            }

            var catSplit = query.FolderId.Split('_');


            if (catSplit[0] == "cat")
            {
                query.FolderId = catSplit[1];
                if (catSplit[1] == "myChannels")
                {
                    return await GetPersonalChannels(query, cancellationToken).ConfigureAwait(false);
                }
                return await GetSubCategories(query, cancellationToken).ConfigureAwait(false);
            }

            if (catSplit[0] == "subcat")
            {
                if (catSplit[1] == "allVideos")
                {
                    //query.FolderId = catSplit[2];
                    return await GetChannelItemsInternal(query, cancellationToken).ConfigureAwait(false);
                }

                query.FolderId = catSplit[1];

                if (catSplit[1] == "allChannels") query.FolderId = catSplit[2];

                return await GetChannels(query, cancellationToken).ConfigureAwait(false);
            }

            return await GetChannelItemsInternal(query, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ChannelItemResult> GetCategories(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var downloader = new VimeoCategoryDownloader(_logger, _jsonSerializer, _httpClient);
            var channels = await downloader.GetVimeoCategoryList(query.StartIndex, query.Limit, cancellationToken);

            if (channels == null)
            {
                channels = new Categories();
            }

            if (Plugin.Instance.Configuration.Token != null && Plugin.Instance.Configuration.SecretToken != null)
            {
                channels.Add(new Category
                {
                    id = "myChannels",
                    name = "My Channels"
                });
            }

            var items = channels.Select(i => new ChannelItemInfo
            {
                Type = ChannelItemType.Folder,
                ImageUrl = i.image,
                Name = i.name,
                Id = "cat_" + i.id,
            }).ToList();

            return new ChannelItemResult
            {
                Items = items,
                TotalRecordCount = channels.total
            };
        }

        private async Task<ChannelItemResult> GetSubCategories(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var downloader = new VimeoCategoryDownloader(_logger, _jsonSerializer, _httpClient);
            var channels = await downloader.GetVimeoSubCategory(query.FolderId, cancellationToken);

            if (channels == null)
            {
                channels = new Category();
            }

            channels.subCategories.Add(new VimeoAPI.API.Channel
            {
                id = "allVideos_" + query.FolderId,
                name = "All Videos"
            });

            channels.subCategories.Add(new VimeoAPI.API.Channel
            {
                id = "allChannels_" + query.FolderId,
                name = "All Channels"
            });

            var items = channels.subCategories.Select(i => new ChannelItemInfo
            {
                Type = ChannelItemType.Folder,
                Name = i.name,
                Id = "subcat_" + i.id,
            }).ToList();

            return new ChannelItemResult
            {
                Items = items
            };
        }

        private async Task<ChannelItemResult> GetChannels(InternalChannelItemQuery query,
            CancellationToken cancellationToken)
        {
            var downloader = new VimeoChannelDownloader(_logger, _jsonSerializer, _httpClient);
            var channels = await downloader.GetVimeoChannelList(query, cancellationToken);

            var items = new List<ChannelItemInfo>();
            var videos = new Videos();

            if (channels != null)
            {
                items = channels.Select(i => new ChannelItemInfo
                {
                    Type = ChannelItemType.Folder,
                    ImageUrl = i.logo_url,
                    Name = i.name,
                    Id = "chan_" + i.id,
                    Overview = i.description
                }).ToList();
            }
            else
            {
                var downloader2 = new VimeoListingDownloader(_logger, _jsonSerializer, _httpClient);
                videos = await downloader2.GetCategoryVideoList(query.FolderId, query.StartIndex, query.Limit, cancellationToken);

                items = videos.Select(i => new ChannelItemInfo
                {
                    ContentType = ChannelMediaContentType.Clip,
                    ImageUrl = i.thumbnails[2].Url,
                    IsInfiniteStream = false,
                    MediaType = ChannelMediaType.Video,
                    Name = i.title,
                    Overview = i.description,
                    Type = ChannelItemType.Media,
                    Id = i.id,
                    RunTimeTicks = TimeSpan.FromSeconds(i.duration).Ticks,
                    Tags = i.tags == null ? new List<string>() : i.tags.Select(t => t.title).ToList(),
                    DateCreated = DateTime.Parse(i.upload_date)
                }).ToList();
            }

            return new ChannelItemResult
            {
                Items = items,
                TotalRecordCount = channels == null ? videos.total : channels.total
            };
        }

        private async Task<ChannelItemResult> GetChannelItemsInternal(InternalChannelItemQuery query,
            CancellationToken cancellationToken)
        {
            var downloader = new VimeoListingDownloader(_logger, _jsonSerializer, _httpClient);
            var catSplit = query.FolderId.Split('_');
            var videos = new Videos();

            if (catSplit.Count() == 3)
            {
                if (catSplit[1] == "allVideos")
                    videos = await downloader.GetCategoryVideoList(catSplit[2], query.StartIndex, query.Limit, cancellationToken).ConfigureAwait(false);
                else
                    videos = await downloader.GetVimeoList(catSplit[2], query.StartIndex, query.Limit, cancellationToken).ConfigureAwait(false);
            }
            else
                videos = await downloader.GetVimeoList(catSplit[1], query.StartIndex, query.Limit, cancellationToken).ConfigureAwait(false);

            var items = videos.Select(i => new ChannelItemInfo
                {
                    ContentType = ChannelMediaContentType.Clip,
                    ImageUrl = i.thumbnails[2].Url,
                    IsInfiniteStream = false,
                    MediaType = ChannelMediaType.Video,
                    Name = i.title,
                    Overview = i.description,
                    Type = ChannelItemType.Media,
                    Id = i.id,
                    RunTimeTicks = TimeSpan.FromSeconds(i.duration).Ticks,
                    Tags = i.tags == null ? new List<string>() : i.tags.Select(t => t.title).ToList(),
                    DateCreated = DateTime.Parse(i.upload_date)

                });

            return new ChannelItemResult
            {
                Items = items.ToList(),
                TotalRecordCount = videos.total
            };
        }

        private async Task<ChannelItemResult> GetPersonalChannels(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var downloader = new VimeoChannelDownloader(_logger, _jsonSerializer, _httpClient);
            var pChannels = await downloader.GetPersonalChannelList(query, cancellationToken);


            var items = pChannels.Select(i => new ChannelItemInfo
            {
                Type = ChannelItemType.Folder,
                ImageUrl = i.logo_url,
                Name = i.name,
                Id = "chan_" + i.id,
                Overview = i.description
            }).ToList();

            return new ChannelItemResult
            {
                Items = items,
                TotalRecordCount = pChannels.total
            };
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Primary:
                    {
                        var path = GetType().Namespace + ".Images." + type.ToString().ToLower() + ".jpg";

                        return Task.FromResult(new DynamicImageResponse
                        {
                            Format = ImageFormat.Jpg,
                            HasImage = true,
                            Stream = GetType().Assembly.GetManifestResourceStream(path)
                        });
                    }
                case ImageType.Backdrop:
                case ImageType.Thumb:
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
                ImageType.Primary,
                ImageType.Backdrop
            };
        }

        public string HomePageUrl
        {
            get { return "https://vimeo.com"; }
        }

        public bool IsEnabledFor(User user)
        {
            return true;
        }

        public string Name
        {
            get { return "Vimeo"; }
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                MaxPageSize = 50,

                ContentTypes = new List<ChannelMediaContentType>
                 {
                     ChannelMediaContentType.Clip
                 },

                MediaTypes = new List<ChannelMediaType>
                  {
                       ChannelMediaType.Video
                  },

                SupportsSortOrderToggle = true,

                DefaultSortFields = new List<ChannelItemSortField>
                   {
                        ChannelItemSortField.Name,
                        ChannelItemSortField.DateCreated,
                        ChannelItemSortField.Runtime
                   }
            };
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            using (var json = await _httpClient.Get(
                "http://player.vimeo.com/video/" + id +
                // "/config?autoplay=0&byline=0&bypass_privacy=1&context=clip.main&default_to_hd=1&portrait=0&title=0",
               "/config?type=moogaloop&referrer=&player_url=player.vimeo.com&v=1.0.0&cdn_url=http://a.vimeocdn.com",
                CancellationToken.None).ConfigureAwait(false))
            {
                var r = _jsonSerializer.DeserializeFromStream<RootObject>(json);

                var mediaInfo = new List<ChannelMediaInfo>();


                if (r.request != null && r.request.files != null)
                {
                    if (r.request.files.h264 != null)
                    {

                        var hd = r.request.files.h264.hd;
                        if (hd != null && !string.IsNullOrEmpty(hd.url))
                        {
                            mediaInfo.Add(
                                new ChannelMediaInfo
                                {
                                    Height = hd.height,
                                    Width = hd.width,
                                    Path = hd.url
                                }
                            );
                        }

                        var sd = r.request.files.h264.sd;
                        if (sd != null && !string.IsNullOrEmpty(sd.url))
                        {
                            mediaInfo.Add(
                                new ChannelMediaInfo
                                {
                                    Height = sd.height,
                                    Width = sd.width,
                                    Path = sd.url
                                }
                             );
                        }

                        var mob = r.request.files.h264.mobile;
                        if (mob != null && !string.IsNullOrEmpty(mob.url))
                        {
                            mediaInfo.Add(
                                new ChannelMediaInfo
                                {
                                    Height = mob.height,
                                    Width = mob.width,
                                    Path = mob.url
                                }
                             );
                        }
                    }
                    else if (r.request.files.vp6 != null)
                    {
                        var sd = r.request.files.vp6.sd;
                        if (sd != null && !string.IsNullOrEmpty(sd.url))
                        {
                            mediaInfo.Add(
                                new ChannelMediaInfo
                                {
                                    Height = sd.height,
                                    Width = sd.width,
                                    Path = sd.url
                                });
                        }
                    }
                }

                return mediaInfo;
            }
        }

        public IOrderedEnumerable<ChannelItemInfo> OrderItems(List<ChannelItemInfo> items, InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            if (query.SortBy.HasValue)
            {
                if (query.SortDescending)
                {
                    switch (query.SortBy.Value)
                    {
                        case ChannelItemSortField.Runtime:
                            return items.OrderByDescending(i => i.RunTimeTicks ?? 0);
                        case ChannelItemSortField.DateCreated:
                            return items.OrderByDescending(i => i.DateCreated ?? DateTime.MinValue);
                        default:
                            return items.OrderByDescending(i => i.Name);
                    }
                }
                else
                {
                    switch (query.SortBy.Value)
                    {
                        case ChannelItemSortField.Runtime:
                            return items.OrderBy(i => i.RunTimeTicks ?? 0);
                        case ChannelItemSortField.DateCreated:
                            return items.OrderBy(i => i.DateCreated ?? DateTime.MinValue);
                        default:
                            return items.OrderBy(i => i.Name);
                    }
                }
            }

            return items.OrderBy(i => i.Name);
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public string Description
        {
            get { return string.Empty; }
        }
    }
}
