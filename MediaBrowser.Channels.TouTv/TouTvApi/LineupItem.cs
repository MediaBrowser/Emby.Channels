using System.Text.RegularExpressions;

namespace MediaBrowser.Channels.TouTv.TouTvApi
{
    internal class LineupItem
    {
        public Details Details { get; set; }
        public string IdMedia { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsFree { get; set; }
        public string Template { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }
}
