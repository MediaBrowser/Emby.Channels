using System.Xml;
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
using System.ServiceModel.Syndication;

namespace MediaBrowser.Channels.iPlayer
{
    public class Channel : IChannel, IRequiresMediaInfoCallback
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;


        private String feedURL = "http://feeds.bbc.co.uk";

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
                return "1";
            }
        }

        public string Description
        {
            get { return string.Empty; }
        }

        public string HomePageUrl
        {
            get { return "http://www.bbc.co.uk"; }
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var menu = new MenuSystem(_httpClient, _jsonSerializer, _logger);

            _logger.Debug("cat ID : " + query.FolderId);

            if (query.FolderId == null)
            {
                return await menu.GetMainMenu(cancellationToken).ConfigureAwait(false);
            }
            
            var folderID = query.FolderId.Split('_');
            query.FolderId = folderID[1];

            if (folderID[0] == "video")
            {
                return await GetVideos(query, cancellationToken).ConfigureAwait(false);
            }
            if (folderID[0] == "tvChannels")
            {
                return await menu.GetTVChannels(cancellationToken).ConfigureAwait(false);
            }
            if (folderID[0] == "categories")
            {
                return await menu.GetCategories("", "", cancellationToken).ConfigureAwait(false);
            }
            if (folderID[0] == "category")
            {

                return await menu.GetCategory(query.FolderId, "", cancellationToken).ConfigureAwait(false);
            }
            if (folderID[0] == "a-z")
            {
                return await menu.GetAToZ(cancellationToken).ConfigureAwait(false);
            }
            

            return null;
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

        private async Task<ChannelItemResult> GetVideos(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var rss = new RSS(query.FolderId, _httpClient, _jsonSerializer, _logger);
            var items = new List<ChannelMediaInfo>();

            await rss.Refresh(cancellationToken);

            return new ChannelItemResult
            {
               // Items = items.ToList()
            };
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            var rss = new RSS(id, _httpClient, _jsonSerializer, _logger);
            var items = new List<ChannelMediaInfo>();

            await rss.Refresh(cancellationToken);

                return items.OrderByDescending(i => i.VideoBitrate ?? 0);

            
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
            get { return "BBC iPlayer"; }
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
