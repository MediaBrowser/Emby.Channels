using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.SoundCloud.ClientApi.Model
{
    public class Origin
    {
        public string artwork_url { get; set; }
        public int comment_count { get; set; }
        public bool commentable { get; set; }
        public string description { get; set; }
        public int download_count { get; set; }
        public bool? downloadable { get; set; }
        public string embeddable_by { get; set; }
        public int favoritings_count { get; set; }
        public string genre { get; set; }
        public string isrc { get; set; }
        public int? label_id { get; set; }
        public string label_name { get; set; }
        public string license { get; set; }
        public int original_content_size { get; set; }
        public string original_format { get; set; }
        public int playback_count { get; set; }
        public string purchase_title { get; set; }
        public string purchase_url { get; set; }
        public string release { get; set; }
        public int? release_day { get; set; }
        public int? release_month { get; set; }
        public int? release_year { get; set; }
        public int reposts_count { get; set; }
        public string state { get; set; }
        public bool streamable { get; set; }
        public string tag_list { get; set; }
        public string track_type { get; set; }
        public MiniUser user { get; set; }
        public int likes_count { get; set; }
        public string attachments_uri { get; set; }
        public double? bpm { get; set; }
        public string key_signature { get; set; }
        public bool? user_favorite { get; set; }
        public int? user_playback_count { get; set; }
        public string video_url { get; set; }
        public int id { get; set; }
        public string kind { get; set; }
        public string created_at { get; set; }
        public string last_modified { get; set; }
        public string permalink { get; set; }
        public string permalink_url { get; set; }
        public string title { get; set; }
        public int duration { get; set; }
        public string sharing { get; set; }
        public string waveform_url { get; set; }
        public string stream_url { get; set; }
        public string uri { get; set; }
        public int user_id { get; set; }
        public string user_uri { get; set; }
        public string secret_uri { get; set; }
        public int? track_count { get; set; }
        public string playlist_type { get; set; }
        public string tracks_uri { get; set; }
        public string download_url { get; set; }
        public string secret_token { get; set; }
        public string type { get; set; }
        public CreatedWith created_with { get; set; }
        public string ean { get; set; }
    }
}
