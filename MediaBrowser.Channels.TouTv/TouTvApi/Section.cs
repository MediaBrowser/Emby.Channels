using System.Collections.Generic;

namespace MediaBrowser.Channels.TouTv.TouTvApi
{
    internal class Section
    {
        public string IdMedia { get; set; }
        public string MediaUrl { get; set; }
        public List<Lineup> SeasonLineups { get; set; }
    }
}
