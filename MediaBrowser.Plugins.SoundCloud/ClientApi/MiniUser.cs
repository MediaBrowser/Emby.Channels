using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.SoundCloud.ClientApi
{
    public class MiniUser
    {
        public string avatar_url { get; set; }
        public int id { get; set; }
        public string kind { get; set; }
        public string permalink_url { get; set; }
        public string uri { get; set; }
        public string username { get; set; }
        public string permalink { get; set; }
        public string last_modified { get; set; }
    }
}
