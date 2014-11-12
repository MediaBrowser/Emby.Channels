using System.Collections.Generic;

namespace MediaBrowser.Channels.LeagueOfLegends.LolEventVods
{
    internal class Day
    {
        public string DayId { get; set; }
        public string Title { get; set; }
        public IEnumerable<Match> Matches { get; set; }
        public string ImageUrl { get; set; }
        public VideoLink FullStream { get; set; }
    }
}