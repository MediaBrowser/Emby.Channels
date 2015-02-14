using System.Collections.Generic;

namespace MediaBrowser.Channels.TouTv.TouTvApi
{
    internal class Lineup
    {
        public string Title { get; set; }
        public List<LineupItem> LineupItems { get; set; }
    }
}
