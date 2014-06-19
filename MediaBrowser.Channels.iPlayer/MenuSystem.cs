using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.iPlayer
{
    internal class MenuSystem
    {
        private String feedURL = "http://feeds.bbc.co.uk";
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        public MenuSystem(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonSerializer = jsonSerializer;
        }

        public async Task<ChannelItemResult> GetMainMenu(CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>
            {
                CreateMenu("Highlights", "video", feedURL + "/iplayer/highlights/tv"),
                CreateMenu("Most Popular", "video", feedURL + "/iplayer/popular/tv"),
                CreateMenu("TV Channels", "tvChannels", ""),
                CreateMenu("Categories", "categories", ""),
                CreateMenu("Formats", "formats", ""),
                CreateMenu("A-Z", "a-z", "")
            };

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        public async Task<ChannelItemResult> GetTVChannels(CancellationToken cancellationToken)
        {
            var data = new Data();
            var channels = data.Channels;

            // Add more items here.
            var items = new List<ChannelItemInfo>();

            foreach (var c in channels)
            {
                items.Add(CreateMenu(c.title, "channel", c.id, c.thumb));
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        public async Task<ChannelItemResult> GetAToZ(CancellationToken cancellationToken)
        {
            var letters = new[]
            {
                "a", "b", "c", "d", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v",
                "w", "x", "y", "z"
            };
            // Add more items here.
            var items = new List<ChannelItemInfo>();

            foreach (var l in letters)
            {
                items.Add(CreateMenu(l.ToUpper(), "video", feedURL + "/iplayer/atoz/" + l + "/list/tv"));
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        public async Task<ChannelItemResult> GetCategories(String channelID, String thumb, CancellationToken cancellationToken)
        {
            var data = new Data();
            var categories = data.Categories;

            // Add more items here.
            var items = new List<ChannelItemInfo>();

            foreach (var c in categories)
            {
                items.Add(CreateMenu(c.title, "category", c.id + "_" + channelID, thumb));
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        public async Task<ChannelItemResult> GetCategory(String categoryID, String channelID, CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>();
            var data = new Data();
            var categories = data.Categories;

            var category = categories.Find(i => i.id == categoryID);

            if (channelID != "")
            {
                // return videos
            }
            else
            {
                items.Add(CreateMenu(category.title + " Highlights", "video", category.highlights_url()));
                items.Add(CreateMenu(category.title + " Popular", "video", category.popular_url()));
                items.Add(CreateMenu("All programmes", "video", feedURL + "/iplayer/highlights/tv"));

                foreach (var subCat in category.subCategories)
                {
                    _logger.Debug("URL : " + category.subcategory_url(subCat.id));
                    items.Add(CreateMenu(subCat.title, "video", category.subcategory_url(subCat.id)));
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        // Create Menu Entry
        private ChannelItemInfo CreateMenu(String title, String menu, String id, String thumb = "")
        {
            return new ChannelItemInfo
            {
                Name = title,
                Type = ChannelItemType.Folder,
                Id = menu + "_" + id,
                ImageUrl = thumb
            };
        }
    }
}
