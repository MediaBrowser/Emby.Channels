using System;

namespace MediaBrowser.Channels.LeagueOfLegends.LolEventVods
{
    internal class Event
    {
        public string Title { get; set; }
        public EventStatus Status { get; set; }
        public string EventId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ImageUrl { get; set; }
    }
}
