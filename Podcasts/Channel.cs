﻿using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using PodCasts.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PodCasts
{
    class Channel : IChannel, IHasCacheKey, ISupportsLatestMedia, IIndexableChannel
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;
        public INotificationManager _notificationManager { get; set; }

        public Channel(IHttpClient httpClient, ILogManager logManager, IProviderManager providerManager, INotificationManager notificationManager)
        {
            _httpClient = httpClient;
            _logger = logManager.GetLogger(GetType().Name);
            _providerManager = providerManager;
            _notificationManager = notificationManager;
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
                await _notificationManager.SendNotification(new NotificationRequest
                {
                    Description = "PodCasts trial has expired.",
                    Date = DateTime.Now,
                    Level = NotificationLevel.Warning,
                    SendToUserMode = SendToUserType.Admins
                }, cancellationToken);

                return new ChannelItemResult
                {
                    Items = items.ToList()
                };
            }

            foreach (var feedUrl in Plugin.Instance.Configuration.Feeds)
            {
                try
                {
                    var document = await GetXDocument(feedUrl, cancellationToken);
                    var feed = CreateFeed(document);
                    items.Add(feed.CreateChannelItemInfo(document.Root, feedUrl));
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting feed", ex);
                }
            }
            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        private string GetValue(XElement element, string name, string namespaceName = null)
        {
            return element.Element(GetXName(name, namespaceName))?.Value;
        }

        private string GetAttribute(XElement element, string name, string namespaceName = null)
        {
            return element.Attribute(GetXName(name, namespaceName))?.Value;
        }

        private XName GetXName(string name, string namespaceName = null)
        {
            return !string.IsNullOrWhiteSpace(namespaceName) ? XName.Get(name, namespaceName)
                                                             : XName.Get(name);
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
            var document = await GetXDocument(feedUrl, cancellationToken);
            var feed = CreateFeed(document);

            if (feed != null)
            {
                return feed.Refresh(document);
            }

            return Enumerable.Empty<ChannelItemInfo>();
        }

        private Feed CreateFeed(XDocument document)
        {
            switch (GetFeedFormat(document))
            {
                case FeedFormat.Atom:
                    return new AtomFeed(_logger);

                case FeedFormat.Rss:
                    return new RssFeed(_logger);

                default:
                    return null;
            }
        }

        private FeedFormat GetFeedFormat(XDocument document)
        {
            if (IsRss(document.Root))
            {
                return FeedFormat.Rss;
            }
            else if (IsAtom(document.Root))
            {
                return FeedFormat.Atom;
            }
            else
            {
                return FeedFormat.Unknown;
            }
        }

        private async Task<XDocument> GetXDocument(string feedUrl, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                Url = feedUrl,
                CancellationToken = cancellationToken,

                // Seeing some deflate stream errors
                EnableHttpCompression = false,
                EnableDefaultUserAgent = true
            };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    return XDocument.Parse(reader.ReadToEnd());
                }
            }
        }

        private bool IsRss(XElement root)
        {
            return root.Name == "rss" && root.Attribute("version") != null;
        }

        private bool IsAtom(XElement root)
        {
            return root.Name == "{http://www.w3.org/2005/Atom}feed";
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

                AutoRefreshLevels = 2,

                SupportsContentDownloading = true
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
                    _notificationManager.SendNotification(new NotificationRequest
                    {
                        Description = "Error getting channel items" + ex,
                        Date = DateTime.Now,
                        Level = NotificationLevel.Error,
                        SendToUserMode = SendToUserType.Admins
                    }, cancellationToken);

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

        private enum FeedFormat
        {
            Atom,
            Rss,
            Unknown
        }
    }
}
