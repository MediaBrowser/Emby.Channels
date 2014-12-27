using System.Collections.Generic;

namespace MediaBrowser.Channels.LeagueOfLegends.LolEventVods
{
    internal class RedditListingData
    {
        public List<RedditPost> Children { get; set; }
        public string After { get; set; }
    }
}