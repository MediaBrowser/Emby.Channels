using System.Collections.Generic;
using System.Linq;
using System.Web;
using HtmlAgilityPack;
using MediaBrowser.Channels.LeagueOfLegends.Twitch;

namespace MediaBrowser.Channels.LeagueOfLegends.LolEventVods
{
    internal class Table
    {
        private readonly HtmlNode _table;

        public Table(HtmlNode table)
        {
            _table = table;
        }

        public string Title
        {
            get
            {
                var titleNode = _table
                    .Descendants("a")
                    .FirstOrDefault(node => node.Attributes["href"].Value == "http://www.table_title.com");
                if (titleNode != null)
                {
                    var titleAttribute = titleNode.Attributes["title"];
                    if (titleAttribute != null)
                    {
                        return titleAttribute.Value;
                    }
                }
                return string.Empty;
            }
        }

        public IEnumerable<Match> Matches
        {
            get
            {
                foreach (var row in FindAllBodyRows())
                {
                    var cells = row.Descendants("td").ToList();
                    // Some matches (like All-Star 1v1 duels) don't have the same HTML layout, just ignore them for now
                    // (they only have Youtube links anyway)
                    if(IsRegularMatch)
                    yield return new Match
                    {
                        GameId = cells[0].InnerText,
                        Team1 = cells[Team1Index].InnerText,
                        Team2 = cells[Team2Index].InnerText,
                        VideoLinks = GetVideoLink(cells)
                    };
                }
            }
        }

        public string FullStreamUrl
        {
            get
            {
                var fullStreamNode = FindFullStreamNode();
                var twitchNode = fullStreamNode
                    .Descendants("a")
                    .FirstOrDefault(node => node.InnerText == "Twitch");
                if (twitchNode != null)
                {
                    return twitchNode.Attributes["href"].Value;
                }
                return string.Empty;
            }
        }

        private bool IsRegularMatch
        {
            get { return GetColumnIndex("team 1").Any(); }
        }

        private int Team1Index
        {
            get { return GetColumnIndex("team 1").First(); }
        }

        private int Team2Index
        {
            get { return GetColumnIndex("team 2").First(); }
        }

        private IEnumerable<int> TwitchIndexes
        {
            get { return GetColumnIndex("twitch"); }
        }

        private IEnumerable<int> GetColumnIndex(string text)
        {
            foreach (var row in FindAllHeadRows())
            {
                var columns = row.Descendants("th");
                int index = 0;
                foreach (var column in columns)
                {
                    if (column.InnerText.ToLower() == text)
                    {
                        yield return index;
                    }
                    index++;
                }
            }
        }

        private IEnumerable<HtmlNode> FindAllHeadRows()
        {
            return _table
                .Descendants("thead")
                .First()
                .Descendants("tr");
        }

        private IEnumerable<HtmlNode> FindAllBodyRows()
        {
            return _table
                .Descendants("tbody")
                .First()
                .Descendants("tr");
        }

        private IEnumerable<VideoLink> GetVideoLink(IList<HtmlNode> cells)
        {
            foreach (var column in TwitchIndexes)
            {
                var linkNode = cells[column].Element("a");
                var url = RemoveSpoilerFreeVideoUrl(linkNode.Attributes["href"].Value);
                var twitchUrlParser = new TwitchUrlParser(url);
                yield return new VideoLink
                {
                    Title = HttpUtility.HtmlDecode(linkNode.InnerText),
                    TwitchId = twitchUrlParser.Id,
                    TimeStart = twitchUrlParser.TimeStart
                };
            }
        }

        private string RemoveSpoilerFreeVideoUrl(string url)
        {
            if (url == "http://lol.eventvods.com")
            {
                return string.Empty;
            }
            return url;
        }

        private HtmlNode FindFullStreamNode()
        {
            HtmlNode result = _table.PreviousSibling;
            while (result.NodeType != HtmlNodeType.Element)
            {
                result = result.PreviousSibling;
            }
            return result;
        }
    }
}
