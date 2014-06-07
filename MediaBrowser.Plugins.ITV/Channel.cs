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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.ITV
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
                return "3";
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
            
            var folderID = query.FolderId.Split('_');
            query.FolderId = folderID[1];

            if (folderID[0] == "programs")
            {
                return await GetProgramList(query, cancellationToken).ConfigureAwait(false);
            }
            if (folderID[0] == "episodes")
            {
                return await GetEpisodeList(query, cancellationToken).ConfigureAwait(false);
            }
            

            return null;
        }

        private async Task<ChannelItemResult> GetMainMenu(CancellationToken cancellationToken)
        {
            // Add more items here.
            var items = new List<ChannelItemInfo>
            {
                new ChannelItemInfo
                {
                    Name = "Most Popular Programmes",
                    Id = "programs_" + "https://www.itv.com/itvplayer/categories/browse/popular/catch-up",
                    Type = ChannelItemType.Folder
                }
            };

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        private async Task<ChannelItemResult> GetProgramList(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();

            using (var site = await _httpClient.Get(query.FolderId, CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);

                foreach (var node in page.DocumentNode.SelectNodes("//div[@id='categories-content']/div[@class='item-list']/ul/li"))
                {
                    // TODO : FIX ME!!!
                    //var thumb = node.SelectSingleNode(".//div[@class='min-container']//img").Attributes["src"].Value.Replace("player_image_thumb_standard", "posterframe");
                    var title = node.SelectSingleNode(".//div[@class='programme-title cell-title']/a").InnerText;
                    var url = "http://www.itv.com" + node.SelectSingleNode(".//div[@class='programme-title cell-title']/a").Attributes["href"].Value;
                   
                    items.Add(new ChannelItemInfo
                    {
                        Name = title,
                        //ImageUrl = thumb,
                        Id = "episodes_" + url,
                        Type = ChannelItemType.Folder
                    });
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        private async Task<ChannelItemResult> GetEpisodeList(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();

            using (var site = await _httpClient.Get(query.FolderId, CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site);

                foreach (var node in page.DocumentNode.SelectNodes("//div[@class='view-content']/div[@class='views-row']"))
                {
                    var id = "http://www.itv.com" + node.SelectSingleNode(".//div[contains(@class, 'node-episode')]/a[1]").Attributes["href"].Value;
                    var title = node.SelectSingleNode("//h2[@class='title episode-title']").InnerText;
                    var seasonNumber = node.SelectSingleNode(".//div[contains(@class, 'field-name-field-season-number')]//text()").InnerText;
                    var episodeNumber = node.SelectSingleNode(".//div[contains(@class, 'field-name-field-episode-number')]//text()").InnerText;
                    var overview = node.SelectSingleNode(".//div[contains(@class,'field-name-field-short-synopsis')]//text()").InnerText;
                    var thumb = node.SelectSingleNode(".//div[contains(@class,'field-name-field-image')]//img").Attributes["src"].Value.Replace("player_image_thumb_standard", "posterframe");
                   
                    items.Add(new ChannelItemInfo
                    {
                        Name = title + " (Season: " + seasonNumber + ", Ep: " + episodeNumber + ")",
                        ImageUrl = thumb,
                        Id = id,
                        Overview = overview,
                        Type = ChannelItemType.Media,
                        ContentType = ChannelMediaContentType.Episode,
                        IsInfiniteStream = false,
                        MediaType = ChannelMediaType.Video,
                    });
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
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
                    html = html.Replace("&#039", "'");

                    var productionNode = Regex.Match(html, "\"productionId\":\"(.*?)\"", RegexOptions.IgnoreCase);
                    var productionID = productionNode.Groups[0].Value;

                    productionID = productionID.Replace(@"\", "");
                    productionID = productionID.Replace("\"productionId\":\"", "");
                    productionID = productionID.Replace("\"", "");

                    _logger.Debug("Production ID : " + productionID);

                    var SM_TEMPLATE =
                        String.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:itv=""http://schemas.datacontract.org/2004/07/Itv.BB.Mercury.Common.Types"" xmlns:com=""http://schemas.itv.com/2009/05/Common"">
	                  <soapenv:Header/>
	                  <soapenv:Body>
		                <tem:GetPlaylist>
		                  <tem:request>
		                <itv:ProductionId>{0}</itv:ProductionId>
		                <itv:RequestGuid>FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF</itv:RequestGuid>
		                <itv:Vodcrid>
		                  <com:Id/>
		                  <com:Partition>itv.com</com:Partition>
		                </itv:Vodcrid>
		                  </tem:request>
		                  <tem:userInfo>
		                <itv:Broadcaster>Itv</itv:Broadcaster>
		                <itv:GeoLocationToken>
		                  <itv:Token/>
		                </itv:GeoLocationToken>
		                <itv:RevenueScienceValue>ITVPLAYER.12.18.4</itv:RevenueScienceValue>
		                <itv:SessionId/>
		                <itv:SsoToken/>
		                <itv:UserToken/>
		                  </tem:userInfo>
		                  <tem:siteInfo>
		                <itv:AdvertisingRestriction>None</itv:AdvertisingRestriction>
		                <itv:AdvertisingSite>ITV</itv:AdvertisingSite>
		                <itv:AdvertisingType>Any</itv:AdvertisingType>
		                <itv:Area>ITVPLAYER.VIDEO</itv:Area>
		                <itv:Category/>
		                <itv:Platform>DotCom</itv:Platform>
		                <itv:Site>ItvCom</itv:Site>
	                  </tem:siteInfo>
	                  <tem:deviceInfo>
		                <itv:ScreenSize>Big</itv:ScreenSize>
	                  </tem:deviceInfo>
	                  <tem:playerInfo>
		                <itv:Version>2</itv:Version>
	                  </tem:playerInfo>
		                </tem:GetPlaylist>
	                  </soapenv:Body>
	                </soapenv:Envelope>
	                ", productionID);

                    // TODO: Need to convert this to httpclient for compatibility

                    var request = (HttpWebRequest)WebRequest.Create("http://mercury.itv.com/PlaylistService.svc");
                    request.ContentType = "text/xml; charset=utf-8";
                    request.ContentLength = SM_TEMPLATE.Length;
                    request.Referer = "http://www.itv.com/mercury/Mercury_VideoPlayer.swf?v=1.6.479/[[DYNAMIC]]/2";
                    request.Headers.Add("SOAPAction", "http://tempuri.org/PlaylistService/GetPlaylist");
                    request.Host = "mercury.itv.com";
                    request.Method = "POST";

                    var requestWriter = new StreamWriter(request.GetRequestStream());

                    try
                    {
                        requestWriter.Write(SM_TEMPLATE);
                    }
                    finally
                    {
                        requestWriter.Close();
                        requestWriter = null;
                    }

                    var response = (HttpWebResponse)request.GetResponse();
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {
                        var videoNode = Regex.Match(sr.ReadToEnd(), "<VideoEntries>(.*?)</VideoEntries>",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
                        var video = videoNode.Groups[0].Value;

                        page.LoadHtml(video);

                        var videoPageNode = page.DocumentNode.SelectSingleNode("/videoentries/video/mediafiles");
                        var rtmp = videoPageNode.Attributes["base"].Value;
                        _logger.Debug(rtmp);

                        foreach (var node in videoPageNode.SelectNodes(".//mediafile"))
                        {
                            var bitrate = node.Attributes["bitrate"].Value;
                            var url = node.SelectSingleNode(".//url").InnerText;
                            var strippedURL = url.Replace("<![CDATA[", "").Replace("]]>", "");

                            var playURL = rtmp + " swfurl=http://www.itv.com/mercury/Mercury_VideoPlayer.swf playpath=" +
                                          strippedURL + " swfvfy=true";

                            items.Add(new ChannelMediaInfo
                            {
                                Path = playURL,
                                VideoBitrate = Convert.ToInt32(bitrate)
                            });
                            _logger.Debug(strippedURL);
                        }
                    }


                    /*var request = new HttpRequestOptions
                    {
                        Url = "http://mercury.itv.com/PlaylistService.svc",
                        Host = "mercury.itv.com",
                        RequestContentType = "text/xml; charset=utf-8",
                        RequestContentBytes = BitConverter.GetBytes(SM_TEMPLATE.Length),
                        RequestContent = SM_TEMPLATE,
                        Referer = "http://www.itv.com/mercury/Mercury_VideoPlayer.swf?v=1.6.479/[[DYNAMIC]]/2"
                    };
                    
                    request.RequestHeaders.Add("SOAPAction", "http://tempuri.org/PlaylistService/GetPlaylist");

                    using (var player = _httpClient.SendAsync(request, "POST"))
                    {
                        using (var reader2 = new StreamReader(site))
                        {
                            var html2 = await reader2.ReadToEndAsync().ConfigureAwait(false);

                            _logger.Debug(html2);
                        }
                    }*/

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
            get { return "ITV UK"; }
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                MaxPageSize = 25,
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
                    ChannelItemSortField.DateCreated,
                    ChannelItemSortField.Name,
                    ChannelItemSortField.Runtime
                },
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
