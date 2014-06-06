using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
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
    class Channel : IChannel, IHasCacheKey
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
                return "3";
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

            foreach (var feedUrl in Plugin.Instance.Configuration.Feeds)
            {
                var feed = new RssFeed(feedUrl);
                await feed.Refresh(_providerManager, _httpClient, cancellationToken).ConfigureAwait(false);

                _logger.Debug(feedUrl);

                var item = new ChannelItemInfo
                {
                    Name = feed.Title,
                    Overview = feed.Description,
                    Id = feedUrl,
                    Type = ChannelItemType.Folder
                };

                if (feed.ImageUrl != null)
                {
                    item.ImageUrl = feed.ImageUrl;
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
            var items = new List<ChannelItemInfo>();

            var feed = new RssFeed(query.FolderId);
            await feed.Refresh(_providerManager, _httpClient, cancellationToken).ConfigureAwait(false);

            foreach (var child in feed.Children.Where(child => string.IsNullOrEmpty(child.PrimaryImagePath)))
            {
                var podcast = child as IHasRemoteImage;

                var item = new ChannelItemInfo
                {
                    Name = child.Name,
                    Overview = feed.Description,
                    ImageUrl = podcast.RemoteImagePath,
                    Id = child.Name,
                    Type = ChannelItemType.Media,
                    ContentType = ChannelMediaContentType.Podcast,
                    MediaType = ChannelMediaType.Audio,

                    MediaSources = new List<ChannelMediaInfo>
                    {
                        new ChannelMediaInfo
                        {
                            Path = child.Path
                        }  
                    }
                };
                    
                items.Add(item);
            }
            return new ChannelItemResult
            {
                Items = items.ToList()
            };
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
                    ChannelMediaType.Audio
                },
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
    }
}
