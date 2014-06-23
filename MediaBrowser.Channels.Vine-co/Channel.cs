using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.Vineco
{
    public class Channel : IChannel, ISupportsLatestMedia
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public Channel(IHttpClient httpClient, ILogManager logManager, IJsonSerializer jsonSerializer)
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
                return "4";
            }
        }

        public string Description
        {
            get { return " Explore a world of beautiful, looping videos"; }
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            
            _logger.Debug("cat ID : " + query.FolderId);

            if (query.FolderId == null)
            {
                return await GetChannels(query, cancellationToken).ConfigureAwait(false);
            }

            var catSplit = query.FolderId.Split('_');

            query.FolderId = catSplit[1];
            /*if (catSplit[0] == "channels")
            {
                return await GetChannels(query, cancellationToken).ConfigureAwait(false);
            }*/

            if (catSplit[0] == "videos")
            {
                return await GetVideos(query, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        // Add when more menus needed
        /*private async Task<ChannelItemResult> GetMainMenu(CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>
            {
                new ChannelItemInfo
                {
                    Name = "Featured Channels",
                    Id = "channels_" + "https://api.vineapp.com/channels/featured",
                    Type = ChannelItemType.Folder
                }
            };

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }*/

        private async Task<ChannelItemResult> GetChannels(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>();

            using (var site = await _httpClient.Get("https://api.vineapp.com/channels/featured", CancellationToken.None).ConfigureAwait(false))
            {
                var channelList = _jsonSerializer.DeserializeFromStream<ChannelList>(site);

                foreach (var c in channelList.data.records)
                {
                    items.Add(new ChannelItemInfo
                    {
                        Name = c.channel,
                        Id = "videos_" + c.channelId,
                        ImageUrl = c.exploreIconFullUrl,
                        Type = ChannelItemType.Folder
                    });
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        private async Task<ChannelItemResult> GetVideos(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>();
            TimelineList timelineList;
            using (var site = await _httpClient.Get(String.Format("https://api.vineapp.com/timelines/channels/{0}/popular?size={1}", query.FolderId, query.Limit), CancellationToken.None).ConfigureAwait(false))
            {
                timelineList = _jsonSerializer.DeserializeFromStream<TimelineList>(site);

                foreach (var c in timelineList.data.records)
                {
                    items.Add(new ChannelItemInfo
                    {
                        Name = c.description,
                        Id = c.postId.ToString(),
                        ImageUrl = c.thumbnailUrl,
                        MediaType = ChannelMediaType.Video,
                        ContentType = ChannelMediaContentType.Clip,
                        Type = ChannelItemType.Media,

                        MediaSources = new List<ChannelMediaInfo>
                        {
                            new ChannelMediaInfo
                            {
                                Path = c.videoUrl,
                                Container = Container.MP4
                            },
                            new ChannelMediaInfo
                            {
                                Path = c.videoLowURL,
                                Container = Container.MP4
                            },
                        }
                    });
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetLatestMedia(ChannelLatestMediaSearch request, CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>();

            using (var site = await _httpClient.Get("https://api.vineapp.com/timelines/popular", CancellationToken.None).ConfigureAwait(false))
            {
                var timelineList = _jsonSerializer.DeserializeFromStream<TimelineList>(site);

                foreach (var c in timelineList.data.records)
                {
                    items.Add(new ChannelItemInfo
                    {
                        Name = c.description,
                        Id = c.postId.ToString(),
                        ImageUrl = c.thumbnailUrl,
                        MediaType = ChannelMediaType.Video,
                        ContentType = ChannelMediaContentType.Clip,
                        Type = ChannelItemType.Media,
                        DateCreated = DateTime.Parse(c.created),

                        MediaSources = new List<ChannelMediaInfo>
                        {
                            new ChannelMediaInfo
                            {
                                Path = c.videoUrl,
                                Container = Container.MP4
                            }
                        }
                    });
                }
            }

            return items.OrderByDescending(i => i.DateCreated).ToList();
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Thumb:
                case ImageType.Backdrop:
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
                ImageType.Backdrop,
                ImageType.Primary
            };
        }

        public string Name
        {
            get { return "Vine"; }
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Clip
                },

                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Video
                },
                MaxPageSize = 100
            };
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public string HomePageUrl
        {
            get { return "http://www.vine.co"; }
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }
    }
}
