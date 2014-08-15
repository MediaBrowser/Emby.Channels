using System.Collections.Generic;

namespace MediaBrowser.Channels.SvtPlay.Entities
{
    internal class Video
    {
        public IList<VideoReference> videoReferences { get; set; }

        public IList<SubtitleReference> subtitleReferences { get; set; }

        public int position { get; set; }

        public int materialLength { get; set; }

        public int livestart { get; set; }

        public bool dvr { get; set; }

        public int livestartUTC { get; set; }

        public bool live { get; set; }

        public bool availableOnMobile { get; set; }
    }

}
