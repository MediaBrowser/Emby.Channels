using System.Collections.Generic;

namespace MediaBrowser.Channels.TouTv.TouTvApi
{
    internal class Details
    {
        public string AirDate { get; set; }
        public string Copyright { get; set; }
        public string Description { get; set; }
        // In seconds
        public int? Length { get; set; }
        public int? ProductionYear { get; set; }
        public List<KeyValuePair<string,string>> Persons { get; set; }
    }
}
