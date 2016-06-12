using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.SoundCloud.ClientApi.Model
{
    public class Activity
    {
        public Origin origin { get; set; }
        public object tags { get; set; }
        public string created_at { get; set; }
        public string type { get; set; }

        public bool IsTrack()
        {
            return type.Contains("track");
        }
        public bool IsPlaylist()
        {
            return type.Contains("playlist");
        }
    }
}
