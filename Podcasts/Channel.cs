using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using PodCasts.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PodCasts
{
    class Channel : IChannel, IHasCacheKey, ISupportsLatestMedia, IIndexableChannel
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;

        public Channel(IHttpClient httpClient, ILogManager logManager, IProviderManager providerManager)
        {
            _httpClient = httpClient;
            _logger = logManager.GetLogger(GetType().Name);
            _providerManager = providerManager;
        }

        public string DataVersion
        {
            get
            {
                // Increment as needed to invalidate all caches
                return "6";
            }
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            _logger.Debug("cat ID : " + query.FolderId);

            if (query.FolderId == null)
            {
                return await GetChannels(cancellationToken).ConfigureAwait(false);
            }

            return await GetChannelItemsInternal(query, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ChannelItemResult> GetChannels(CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>();

            if (!Plugin.Instance.Registration.IsValid)
            {
                Plugin.Logger.Warn("PodCasts trial has expired.");
                return new ChannelItemResult
                {
                    Items = items.ToList()
                };
            }

            foreach (var feedUrl in Plugin.Instance.Configuration.Feeds)
            {
                var feed = await new RssFeed().GetFeed(_providerManager, _httpClient, feedUrl, cancellationToken).ConfigureAwait(false);

                _logger.Debug(feedUrl);

                var item = new ChannelItemInfo
                {
                    Name = feed.Title == null ? null : feed.Title.Text,
                    Overview = feed.Description == null ? null : feed.Description.Text,
                    Id = feedUrl,
                    Type = ChannelItemType.Folder
                };

                if (feed.ImageUrl != null)
                {
                    item.ImageUrl = feed.ImageUrl.AbsoluteUri;
                }

                items.Add(item);
            }
            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        private async Task<ChannelItemResult> GetChannelItemsInternal(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var items = await GetChannelItemsInternal(query.FolderId, cancellationToken).ConfigureAwait(false);

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
                            items = items.OrderByDescending(i => i.DateCreated ?? DateTime.MinValue);
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
                            items = items.OrderBy(i => i.DateCreated ?? DateTime.MinValue);
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

            var list = items.ToList();

            return new ChannelItemResult
            {
                Items = list,
                TotalRecordCount = list.Count
            };
        }

        private async Task<IEnumerable<ChannelItemInfo>> GetChannelItemsInternal(string feedUrl, CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>();

            var rssItems = await new RssFeed().Refresh(_providerManager, _httpClient, feedUrl, cancellationToken).ConfigureAwait(false);

            foreach (var child in rssItems)
            {
                var podcast = (IHasRemoteImage)child;

                var item = new ChannelItemInfo
                {
                    Name = child.Name,
                    Overview = child.Overview,
                    ImageUrl = podcast.RemoteImagePath ?? "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/Podcasts/Images/thumb.png",
                    Id = child.Id.ToString("N"),
                    Type = ChannelItemType.Media,
                    ContentType = ChannelMediaContentType.Podcast,
                    MediaType = child is Video ? ChannelMediaType.Video : ChannelMediaType.Audio,

                    MediaSources = new List<ChannelMediaInfo>
                    {
                        new ChannelMediaInfo
                        {
                            Path = child.Path
                        }  
                    },

                    DateCreated = child.DateCreated,
                    PremiereDate = child.PremiereDate,

                    RunTimeTicks = child.RunTimeTicks,
                    OfficialRating = child.OfficialRating
                };

                items.Add(item);
            }
            return items;
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Thumb,
                ImageType.Backdrop
            };
        }

        public string HomePageUrl
        {
            get { return ""; }
        }

        public string Name
        {
            get { return "Podcasts"; }
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Podcast
                },

                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Audio,
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

                AutoRefreshLevels = 2
            };
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Thumb:
                case ImageType.Backdrop:
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

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public string GetCacheKey(string userId)
        {
            return string.Join(",", Plugin.Instance.Configuration.Feeds.ToArray());
        }

        public string Description
        {
            get { return string.Empty; }
        }

        public async Task<ChannelItemResult> GetAllMedia(InternalAllChannelMediaQuery query, CancellationToken cancellationToken)
        {
            if (query.ContentTypes.Length > 0 && !query.ContentTypes.Contains(ChannelMediaContentType.Podcast))
            {
                return new ChannelItemResult();
            }

            var tasks = Plugin.Instance.Configuration.Feeds.Select(async i =>
            {

                try
                {
                    return await GetChannelItemsInternal(i, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Plugin.Logger.ErrorException("Error getting channel items", ex);

                    return new List<ChannelItemInfo>();
                }
            });

            var items = (await Task.WhenAll(tasks).ConfigureAwait(false))
                .SelectMany(i => i);

            if (query.ContentTypes.Length > 0)
            {
                items = items.Where(i => query.ContentTypes.Contains(i.ContentType));
            }

            var all = items.ToList();

            return new ChannelItemResult
            {
                Items = all,
                TotalRecordCount = all.Count
            };
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetLatestMedia(ChannelLatestMediaSearch request, CancellationToken cancellationToken)
        {
            // Looks like the only way we can do this is by getting all, then sorting

            var all = await GetAllMedia(new InternalAllChannelMediaQuery
            {
                UserId = request.UserId

            }, cancellationToken).ConfigureAwait(false);

            return all.Items.OrderByDescending(i => i.DateCreated ?? DateTime.MinValue);
        }
    }
}
