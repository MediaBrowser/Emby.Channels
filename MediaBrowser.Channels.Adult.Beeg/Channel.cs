using HtmlAgilityPack;
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

namespace MediaBrowser.Channels.Adult.Beeg
{
    public class Channel : IChannel, IRequiresMediaInfoCallback
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public Channel(IHttpClient httpClient, ILogManager logManager)
        {
            _httpClient = httpClient;
            _logger = logManager.GetLogger(GetType().Name);
        }

        public string DataVersion
        {
            get
            {
                // Increment as needed to invalidate all caches
                return "2";
            }
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            
            _logger.Debug("cat ID : " + query.FolderId);

            if (query.FolderId == null)
            {
                return await GetMainMenu(cancellationToken).ConfigureAwait(false);
            }

            var catSplit = query.FolderId.Split('_');

            query.FolderId = catSplit[1];
            if (catSplit[0] == "video")
            {
                return await GetVideos(query, cancellationToken).ConfigureAwait(false);
            }

            if (catSplit[0] == "tags")
            {
                return await GetTags(cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        private async Task<ChannelItemResult> GetMainMenu(CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>
            {
                new ChannelItemInfo
                {
                    Name = "Browse All Videos",
                    Id = "video_" + "http://www.beeg.com/section/home/",
                    Type = ChannelItemType.Folder,
                    OfficialRating = "GB-18"
                },
                new ChannelItemInfo
                {
                    Name = "Browse Videos by Tag",
                    Id = "tags_",
                    Type = ChannelItemType.Folder,
                    OfficialRating = "GB-18"
                }
            };

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        private async Task<ChannelItemResult> GetTags(CancellationToken cancellationToken)
        {   
            var items = new List<ChannelItemInfo>();
            var page = new HtmlDocument();

            using (var site = await _httpClient.Get("http://www.beeg.com/", CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                if (page.DocumentNode != null)
                {
                    foreach (var node in page.DocumentNode.SelectNodes("//a[contains(@href, \"tag\")]"))
                    {
                        var title = node.InnerText;
                        var url = node.Attributes["href"].Value;

                        items.Add(new ChannelItemInfo
                        {
                            Name = title,
                            Id = "video_" + "http://www.beeg.com" + url,
                            Type = ChannelItemType.Folder,
                            OfficialRating = "GB-18"
                        });
                    }
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

            using (var site = await _httpClient.Get(query.FolderId, CancellationToken.None).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(site))
                {
                    var html = await reader.ReadToEndAsync().ConfigureAwait(false);
                    var videoIDNode = Regex.Match(html, @"var tumbid.*=.*\[(.+?)\];");
                    var videoIDs = videoIDNode.Groups[0].Value.Replace("var tumbid  =[", "").Replace("];", "").Split(',');

                    var videoNameNode = Regex.Match(html, @"var tumbalt.*=.*\[(.+?)\];");
                    var videoNames = videoNameNode.Groups[0].Value.Replace("var tumbalt  =[", "").Replace("];", "").Split(',');

                    for (var x = 0; x < videoIDs.Count(); x++)
                    {
                        items.Add(new ChannelItemInfo
                        {
                            Type = ChannelItemType.Media,
                            ContentType = ChannelMediaContentType.Clip,
                            MediaType = ChannelMediaType.Video,
                            ImageUrl = "http://cdn.anythumb.com/640x360/" + videoIDs[x] + ".jpg",
                            Name = videoNames[x],
                            Id = videoIDs[x]
                        });
                    }
                   
                }
            }
           
            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            using (var site = await _httpClient.Get("http://www.beeg.com/" + id, CancellationToken.None).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(site))
                {
                    var html = await reader.ReadToEndAsync().ConfigureAwait(false);
                    var videoNode = Regex.Match(html, @"'file': '([^']+)'");
                    var video = videoNode.Groups[0].Value.Replace("'file': '", "").Replace("'", "");

                    return new List<ChannelMediaInfo>
                    {
                        new ChannelMediaInfo
                        {
                            Path = video,
                            AudioChannels = 2,
                            VideoCodec = VideoCodec.H264,
                            AudioCodec = AudioCodec.AAC,
                            Container = Container.MP4
                        }
                    };
                }
            }
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
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
                ImageType.Thumb
            };
        }

        public string Name
        {
            get { return "Beeg"; }
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
            };
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public string HomePageUrl
        {
            get { return "http://www.beeg.com"; }
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.Adult; }
        }
    }
}
