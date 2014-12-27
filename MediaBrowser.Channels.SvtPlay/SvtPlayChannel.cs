using HtmlAgilityPack;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Channels.SvtPlay
{
    public class SvtPlayChannel : IChannel, IRequiresMediaInfoCallback, ISupportsLatestMedia
    {
        private const string BASE_URL = "http://www.svtplay.se";
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;


        public string DataVersion
        {
            get
            {
                // Increment as needed to invalidate all caches 
                return "5";
            }
        }

        public string Description
        {
            get { return string.Empty; }
        }

        public SvtPlayChannel(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            _httpClient = httpClient;
            _logger = logManager.GetLogger(GetType().Name);
            _jsonSerializer = jsonSerializer;
        }

        public string HomePageUrl
        {
            get { return "http://www.svtplay.se"; }
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public string Name
        {
            get { return "SVT Play"; }
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                MaxPageSize = 25,
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Clip,
                    ChannelMediaContentType.Episode,
                    ChannelMediaContentType.Movie
                },

                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Video
                },

                SupportsSortOrderToggle = false,

                DefaultSortFields = new List<ChannelItemSortField>
                {
                    ChannelItemSortField.DateCreated,
                    ChannelItemSortField.Name,
                    ChannelItemSortField.Runtime
                },
            };
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

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Thumb:
                case ImageType.Primary:
                case ImageType.Backdrop:
                    {
                        var path = GetType().Namespace + ".Images." + type.ToString().ToLower() + ".png";

                        _logger.Log(LogSeverity.Info, "Trying to get image from path: {0}", path);

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

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, System.Threading.CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(query.FolderId))
            {
                var menu = await GetMenu("/");
                return new ChannelItemResult { Items = menu, TotalRecordCount = menu.Count };
            }
            else
            {
                _logger.Info("Received query:{0}", query.FolderId);
                var properties = query.FolderId.Split('_');


                List<ChannelItemInfo> items = new List<ChannelItemInfo>();
                if (properties.Length == 1)
                {
                    items = await GetMenu(properties[0]);
                }
                else
                {
                    items = await GetChildren(properties[0], properties[1], cancellationToken);
                }
                var total = items.Count;

                if (query.StartIndex > 0 && query.StartIndex < items.Count)
                {
                    items.RemoveRange(0, query.StartIndex.Value);
                }

                if (query.Limit.HasValue && items.Count > query.Limit)
                {
                    int toMany = items.Count - query.Limit.Value;
                    items.RemoveRange(query.Limit.Value, toMany - 1);
                }

                return new ChannelItemResult { Items = items, TotalRecordCount = total };
            }
        }

        private async Task<List<ChannelItemInfo>> GetChildren(string query, string url, CancellationToken token)
        {
            var doc = new HtmlDocument();
            using (var stream = await _httpClient.Get(BASE_URL + url, CancellationToken.None).ConfigureAwait(false))
            {
                doc.Load(stream, Encoding.UTF8);

                _logger.Info("Parsing Query: {0}", query);

                var abroadOnly = SvtPlay.Plugin.Instance.Configuration.AvailableAbroadOnly.GetValueOrDefault();

                switch (query)
                {
                    case "recommended":
                        return SvtPlaySiteParser.ParseNode(doc.DocumentNode.SelectSingleNode("//section[contains(@id, 'recommended-videos')]"), abroadOnly);
                    case "popular":
                        return SvtPlaySiteParser.ParseNode(doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'popular-videos')]"), abroadOnly);
                    case "latest":
                        return SvtPlaySiteParser.ParseNode(doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'latest-videos')]"), abroadOnly);
                    case "lastChance":
                        return SvtPlaySiteParser.ParseNode(doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'last-chance-videos')]"), abroadOnly);
                    case "live":
                        return SvtPlaySiteParser.ParseNode(doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'live-channels')]"), abroadOnly);
                    case "categories":
                        return SvtPlaySiteParser.ParseNode(doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'categories')]"), abroadOnly);
                    default:
                        return SvtPlaySiteParser.ParseNode(null, abroadOnly);
                }
            }
        }

        private async Task<List<ChannelItemInfo>> GetMenu(string url)
        {
            var doc = new HtmlDocument();
            using (var stream = await _httpClient.Get(BASE_URL + url, CancellationToken.None).ConfigureAwait(false))
            {
                _logger.Warn("Stream lenght: {0}", stream.Length);
                doc.Load(stream, Encoding.UTF8);
            }

            var abroadOnly = SvtPlay.Plugin.Instance.Configuration.AvailableAbroadOnly.GetValueOrDefault();

            var recommended = SvtPlaySiteParser.ParseNode(doc.DocumentNode.SelectSingleNode("//section[contains(@id, 'recommended-videos')]"), abroadOnly);
            var popular = SvtPlaySiteParser.ParseNode(doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'popular-videos')]"), abroadOnly);
            var latest = SvtPlaySiteParser.ParseNode(doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'latest-videos')]"), abroadOnly);
            var lastChance = SvtPlaySiteParser.ParseNode(doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'last-chance-videos')]"), abroadOnly);
            var live = SvtPlaySiteParser.ParseNode(doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'live-channels')]"), abroadOnly);
            var catgories = SvtPlaySiteParser.ParseNode(doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'categories')]"), abroadOnly);

            var items = new List<ChannelItemInfo>();

            if (recommended.Any())
                items.Add(SvtPlaySiteParser.CreateFolderItem("Recommended", "recommended", url, recommended.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.ImageUrl))));
            if (popular.Any())
                items.Add(SvtPlaySiteParser.CreateFolderItem("Popular", "popular", url, popular.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.ImageUrl))));
            if (latest.Any())
                items.Add(SvtPlaySiteParser.CreateFolderItem("Latest", "latest", url, latest.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.ImageUrl))));
            if (lastChance.Any())
                items.Add(SvtPlaySiteParser.CreateFolderItem("Last Chance", "lastChance", url, lastChance.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.ImageUrl))));
            //if (live.Any())
            //    items.Add(CreateBaseFolder("live", url, live.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.ImageUrl))));
            if (catgories.Any())
                items.Add(SvtPlaySiteParser.CreateFolderItem("Categories", "categories", url, catgories.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.ImageUrl))));
            return items;
        }    

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            var items = new List<ChannelMediaInfo>();
            using (var streaminfo = await _httpClient.Get(string.Format("http://www.svtplay.se{0}?output=json", id.ToLower()), cancellationToken))
            {
                var response = _jsonSerializer.DeserializeFromStream<SvtPlay.Entities.SvtPlayResponse>(streaminfo);
                var mediasource = response.video.videoReferences.FirstOrDefault(r => r.playerType == "ios");

                var info = new ChannelMediaInfo
                {
                    Path = mediasource.url,
                    RunTimeTicks = TimeSpan.FromSeconds(response.video.materialLength).Ticks
                };

                items.Add(info);
            }

            return items;

        }

        public async Task<IEnumerable<ChannelItemInfo>> GetLatestMedia(ChannelLatestMediaSearch request, CancellationToken cancellationToken)
        {
            return await GetChildren("latest", "/", cancellationToken).ConfigureAwait(false);
        }
    }
}
