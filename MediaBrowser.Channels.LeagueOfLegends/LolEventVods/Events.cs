using System.Collections.Generic;

namespace MediaBrowser.Channels.LeagueOfLegends.LolEventVods
{
    internal class Events
    {
        public IEnumerable<Event> Items { get; set; }
        public string After { get; set; }
    }
}