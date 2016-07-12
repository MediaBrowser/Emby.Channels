using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.SoundCloud.ClientApi.Model
{
    public class Playlist
    {
        public string kind { get; set; }
        public int id { get; set; }
        public string created_at { get; set; }
        public int user_id { get; set; }
        public int duration { get; set; }
        public string sharing { get; set; }
        public string tag_list { get; set; }
        public string permalink { get; set; }
        public int track_count { get; set; }
        public bool streamable { get; set; }
        public bool? downloadable { get; set; }
        public string embeddable_by { get; set; }
        public string purchase_url { get; set; }
        public int? label_id { get; set; }
        public string type { get; set; }
        public string playlist_type { get; set; }
        public string ean { get; set; }
        public string description { get; set; }
        public string genre { get; set; }
        public string release { get; set; }
        public string purchase_title { get; set; }
        public string label_name { get; set; }
        public string title { get; set; }
        public int? release_year { get; set; }
        public int? release_month { get; set; }
        public int? release_day { get; set; }
        public string license { get; set; }
        public string uri { get; set; }
        public string permalink_url { get; set; }
        public string artwork_url { get; set; }
        public MiniUser user { get; set; }
        public CreatedWith created_with { get; set; }
        public Label label { get; set; }
        public string last_modified { get; set; }
        public Track[] tracks { get; set; }
    }
}
