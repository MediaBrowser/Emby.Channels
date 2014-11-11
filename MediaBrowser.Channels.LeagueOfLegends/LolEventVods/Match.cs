using System.Collections.Generic;

namespace MediaBrowser.Channels.LeagueOfLegends.LolEventVods
{
    internal class Match
    {
        public string GameId { get; set; }
        public string Team1 { get; set; }
        public string Team2 { get; set; }
        public IEnumerable<VideoLink> VideoLinks { get; set; }
    }
}
