using System.Collections.Generic;
using System.Linq;
using System.Web;
using HtmlAgilityPack;

namespace MediaBrowser.Channels.LeagueOfLegends.LolEventVods
{
    internal class RedditHtmlParser
    {
        private readonly HtmlDocument _htmlDoc;

        internal RedditHtmlParser(string html)
        {
            _htmlDoc = new HtmlDocument();
            var decodedHtml = HttpUtility.HtmlDecode(html);
            _htmlDoc.LoadHtml(decodedHtml);
        }

        public string ImageUrl
        {
            get
            {
                var pictureNode = _htmlDoc
                    .DocumentNode
                    .Descendants("a")
                    .FirstOrDefault(node => node.Attributes["href"].Value == "#EVENT_PICTURE");
                if (pictureNode != null)
                {
                    var titleAttribute = pictureNode.Attributes["title"];
                    if (titleAttribute != null)
                    {
                        return titleAttribute.Value;
                    }
                }
                return string.Empty;
            }
        }

        public bool IsEvent
        {
            get
            {
                var titleNode = _htmlDoc
                    .DocumentNode
                    .Descendants("a")
                    .FirstOrDefault(node => node.Attributes["href"].Value == "#EVENT_TITLE");
                return titleNode != null;
            }
        }

        public IEnumerable<Table> Tables
        {
            get
            {
                var tables = _htmlDoc
                    .DocumentNode
                    .Descendants("table");
                return tables.Select(table => new Table(table));
            }
        }
    }
}
