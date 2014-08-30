
using System;

namespace MediaBrowser.Channels.SvtPlay.Entities
{

    internal class SvtPlayResponse
    {
        public int videoId { get; set; }

        public Video video { get; set; }

        public Context context { get; set; }
        
        public string disabled { get; set; }
    }

}
