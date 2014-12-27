using HtmlAgilityPack;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
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

namespace MediaBrowser.Channels.Adult.RedTube
{
    public class Channel : IChannel, IRequiresMediaInfoCallback
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        public Channel(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
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
                return "7";
            }
        }

        public string Description
        {
            get { return string.Empty; }
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            _logger.Debug("cat ID : " + query.FolderId);

            if (query.FolderId == null)
            {
                return await GetMainMenu(cancellationToken).ConfigureAwait(false);
            }

            var catSplit = query.FolderId.Split('_');

            if (catSplit[0] == "categories")
            {
                return await GetCategories(cancellationToken).ConfigureAwait(false);
            }

            query.FolderId = catSplit[1];
            
            if (catSplit[0] == "videos")
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
                    Name = "Categories",
                    Id = "categories_",
                    Type = ChannelItemType.Folder
                }
            };

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        private async Task<ChannelItemResult> GetCategories(CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>();

            using (var site = await _httpClient.Get("http://api.redtube.com/?data=redtube.Categories.getCategoriesList&output=json", CancellationToken.None).ConfigureAwait(false))
            {
                var categories = _jsonSerializer.DeserializeFromStream<RootObject>(site);

                foreach (var c in categories.categories)
                {
                    if (c.category != "japanesecensored")
                    {
                        items.Add(new ChannelItemInfo
                        {
                            Name = c.category.Substring(0, 1).ToUpper() + c.category.Substring(1),
                            Id = "videos_" + c.category,
                            Type = ChannelItemType.Folder,
                            ImageUrl =
                                "http://img.l3.cdn.redtubefiles.com/_thumbs/categories/categories-180x135/" + c.category +
                                "_001.jpg"
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
            int total;

            int? page = null;

            if (query.StartIndex.HasValue && query.Limit.HasValue)
            {
                page = 1 + (query.StartIndex.Value / query.Limit.Value) % query.Limit.Value;
            }

            using (var site = await _httpClient.Get(String.Format("http://api.redtube.com/?data=redtube.Videos.searchVideos&output=json&category={0}&thumbsize=large&page={1}", query.FolderId, page), CancellationToken.None).ConfigureAwait(false))
            {
                var videos = _jsonSerializer.DeserializeFromStream<RootObject>(site);

                total = videos.count;

                foreach (var v in videos.videos)
                {
                    var durationNode = v.video.duration.Split(':');
                    _logger.Debug(durationNode[0] + "." + durationNode[1]);
                    var time = Convert.ToDouble(durationNode[0] + "." + durationNode[1]);

                    items.Add(new ChannelItemInfo
                    {
                        Type = ChannelItemType.Media,
                        ContentType = ChannelMediaContentType.Clip,
                        MediaType = ChannelMediaType.Video,
                        ImageUrl = v.video.default_thumb.Replace("m.jpg", "b.jpg"),
                        Name = v.video.title,
                        Id = v.video.url,
                        RunTimeTicks = TimeSpan.FromMinutes(time).Ticks,
                        //Tags = v.video.tags == null ? new List<string>() : v.video.tags.Select(t => t.title).ToList(),
                        DateCreated = DateTime.Parse(v.video.publish_date),
                        CommunityRating = float.Parse(v.video.rating)

                    });
                }
            }
           
            return new ChannelItemResult
            {
                Items = items.ToList(),
                TotalRecordCount = total
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


        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            using (var site = await _httpClient.Get(id, CancellationToken.None).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(site))
                {
                    var html = await reader.ReadToEndAsync().ConfigureAwait(false);
                    var videoNode = Regex.Match(html, "<source src=\"([^']+)\" type=\"video/mp4\">");
                    var video = videoNode.Groups[0].Value.Replace("<source src=\"", "").Replace("\" type=\"video/mp4\">", "");

                    return new List<ChannelMediaInfo>
                    {
                        new ChannelMediaInfo
                        {
                            Path = video,
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
            get { return "RedTube"; }
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
                MaxPageSize = 20
            };
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public string HomePageUrl
        {
            get { return "http://www.redtube.com"; }
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.Adult; }
        }
    }
}
