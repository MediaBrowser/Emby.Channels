using System.Runtime.CompilerServices;
using System.Xml;
using HtmlAgilityPack;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.CBS
{
    public class Channel : IChannel, IRequiresMediaInfoCallback
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
                return "10";
            }
        }

        public string Description
        {
            get { return string.Empty; }
        }

        public string HomePageUrl
        {
            get { return "http://www.itv.com"; }
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            _logger.Debug("cat ID : " + query.FolderId);

            if (query.FolderId == null)
            {
                return await GetMainMenu(cancellationToken).ConfigureAwait(false);
            }

            var folderID = query.FolderId.Split(new[] { "!_" }, StringSplitOptions.None);
            query.FolderId = folderID[1];

            if (folderID[0] == "shows")
            {
                return await GetShowList(query, cancellationToken).ConfigureAwait(false);
            }
            if (folderID[0] == "category")
            {
                return await GetCategoryList(query, cancellationToken).ConfigureAwait(false);
            }
            if (folderID[0] == "video")
            {
                return await GetVideoList(query, cancellationToken).ConfigureAwait(false);
            }
            return null;
        }

        private async Task<ChannelItemResult> GetMainMenu(CancellationToken cancellationToken)
        {
            var data = new Data();
            var items = new List<ChannelItemInfo>();

            foreach (var d in data.Categories)
            {
                items.Add(new ChannelItemInfo
                {
                    Name = d.name,
                    Id = "shows!_" + d.id,
                    Type = ChannelItemType.Folder
                    // Add thumb
                });
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        private async Task<ChannelItemResult> GetShowList(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>();

            using (var site = await _httpClient.Get(String.Format("http://www.cbs.com/carousels/showsByCategory/{0}/offset/0/limit/99/", query.FolderId), CancellationToken.None).ConfigureAwait(false))
            {
                var showList = _jsonSerializer.DeserializeFromStream<ShowList>(site);

                foreach (var c in showList.result.data)
                {
                    if ((c.filepath_ipad == "") || (c.filepath_show_logo == "")) continue;
                    if ((c.title == "Live On Letterman") || (c.title == "The CBS Dream Team...It's Epic")) continue;

                    var url = c.link;
                    if (!url.Contains("/video")) url = url + "video";
                    var thumb = c.filepath_ipad;

                    items.Add(new ChannelItemInfo
                    {
                        Name = c.title,
                        ImageUrl = thumb,
                        Id = "category!_" + url,
                        Type = ChannelItemType.Folder
                    });
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        private async Task<ChannelItemResult> GetCategoryList(InternalChannelItemQuery query,
            CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();

            using (var site = await _httpClient.Get(query.FolderId, CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site);

                foreach (var nodes in page.DocumentNode.SelectNodes("//div[starts-with(@id, \"id-carousel\")]"))
                {
                    var id = nodes.Attributes["id"].Value;
                    _logger.Debug("Past ID " + id);
                    var idSplit = id.Split('-');

                    _logger.Debug(idSplit[0] + "  " + idSplit[1] + "  " + idSplit[2]);

                    var url = String.Format("http://www.cbs.com/carousels/videosBySection/{0}/offset/0/limit/15/xs/0",
                        idSplit[2]);
                    
                    using (var json = await _httpClient.Get(url,
                                    CancellationToken.None).ConfigureAwait(false))
                    {
                        var catList = _jsonSerializer.DeserializeFromStream<CategoryList>(json);

                        items.Add(new ChannelItemInfo
                        {
                            Name = catList.result.title,
                            Id = "video!_" + url,
                            Type = ChannelItemType.Folder
                        });
                    }
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        private async Task<ChannelItemResult> GetVideoList(InternalChannelItemQuery query,
            CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();
            _logger.Debug("URL ! : " + query.FolderId);
            using (var json = await _httpClient.Get(query.FolderId, CancellationToken.None).ConfigureAwait(false))
            {
                var videoList = _jsonSerializer.DeserializeFromStream<CategoryList>(json);

                foreach (var v in videoList.result.data)
                {
                    var t = v.title;
                    var thumb = (v.thumb.large ?? v.thumb.small);
                    var season = v.season_number;
                    var episode = v.episode_number;
                    var overview = v.description;

                    _logger.Debug(v.airdate);
                    //var date = DateTime.Parse(v.airdate);
                    var url = v.url;

                    if (v.type != "Full Episode")
                    {
                        _logger.Debug("URL 3 ! : " + url);

                        items.Add(new ChannelItemInfo
                        {
                            Name = t,
                            Id = "http://www.cbs.com" + url,
                            Type = ChannelItemType.Media,
                            MediaType = ChannelMediaType.Video,
                            ContentType = ChannelMediaContentType.Clip,
                            //PremiereDate = date,
                            ImageUrl = thumb,
                            Overview = overview
                        });
                    }
                    else
                    {
                        var show = v.series_title;
                        _logger.Debug("URL 2 ! : " + url);
                        using (
                            var site = await _httpClient.Get("http://www.cbs.com" + url, CancellationToken.None).ConfigureAwait(false))
                        {
                            page.Load(site);

                            _logger.Debug("URL 2 ! : " + url);
                            items.Add(new ChannelItemInfo
                            {
                                Name = show + " " + t,
                                Id = "http://www.cbs.com" + url,
                                Type = ChannelItemType.Media,
                                MediaType = ChannelMediaType.Video,
                                ContentType = ChannelMediaContentType.Clip,
                                //PremiereDate = date,
                                ImageUrl = thumb,
                                Overview = overview,
                            });

                        }
                    }
                }


                return new ChannelItemResult
                {
                    Items = items.ToList()
                };
            }
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelMediaInfo>();
            using (var site = await _httpClient.Get(id, CancellationToken.None).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(site))
                {
                    var html = await reader.ReadToEndAsync().ConfigureAwait(false);
                    
                    var productionNode = Regex.Match(html, "video.settings.pid = '(?<pid>[^']+)';", RegexOptions.IgnoreCase);
                    var productionID = productionNode.Groups["pid"].Value;

                    _logger.Debug("Production ID : " + productionID);

                    using (var xmlSite = await _httpClient.Get("http://link.theplatform.com/s/dJ5BDC/" + productionID + "?format=SMIL&Tracking=true&mbr=true", CancellationToken.None).ConfigureAwait(false))
                    {
                        page.Load(xmlSite);

                        var geoNode = page.DocumentNode.SelectSingleNode("//seq/ref[@title=\"Geographic Restriction\"]");

                        if (geoNode == null)
                        {
                            items.Add(new ChannelMediaInfo
                            {
                                Path = geoNode.Attributes["src"].Value,
                                Protocol = MediaProtocol.Http
                            });
                            return items;
                        }
                        
                        
                        var rtmp_urlNode = page.DocumentNode.SelectSingleNode("//meta[starts-with(@base, \"rtmp\")]");
                        var rtmp_url = "";
                        if (rtmp_urlNode != null)
                        {
                            rtmp_url = rtmp_urlNode.Attributes["base"].Value.Replace("&amp;", "&");
                            _logger.Debug(rtmp_url);
                        }

                        foreach (var node in page.DocumentNode.SelectNodes("//switch/video"))
                        {
                            var url = node.Attributes["src"].Value;
                            var bitrate = node.Attributes["system-bitrate"].Value;
                            var width = node.Attributes["width"].Value;
                            var height = node.Attributes["height"].Value;

                            var playURL = rtmp_url + " swfurl=http://www.cbs.com/thunder/player/1_0/chromeless/1_5_1/CAN.swf playpath=" +
                                          url + " swfvfy=true";

                            items.Add(new ChannelMediaInfo
                            {
                                Path = playURL,
                                VideoBitrate = Convert.ToInt32(bitrate),
                                Width = Convert.ToInt16(width),
                                Height = Convert.ToInt16(height),
                                Protocol = MediaProtocol.Rtmp,
                                ReadAtNativeFramerate = true
                            });
                        }

                    }
                }

                return items.OrderByDescending(i => i.VideoBitrate ?? 0);

            }
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


        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Thumb,
                ImageType.Backdrop
            };
        }

        public string Name
        {
            get { return "CBS"; }
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
                }
            };
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }
    }
}
