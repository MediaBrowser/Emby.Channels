using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Channels.Vevo
{
    class Info
    {
        public class ArtistsMain
        {
            public bool on_tour { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public string image_url { get; set; }
            public string url_safename { get; set; }
            public int favoritecount { get; set; }
            public int video_count { get; set; }
        }

        public class Credit
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }

        public class Artist
        {
            public bool on_tour { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public string image_url { get; set; }
            public string url_safename { get; set; }
            public int favoritecount { get; set; }
            public int video_count { get; set; }
        }

        public class Result
        {
            public string isrc { get; set; }
            public string title { get; set; }
            public string image_url { get; set; }
            public int duration_in_seconds { get; set; }
            public List<ArtistsMain> artists_main { get; set; }
            public List<ArtistsMain> artists_featured { get; set; }
            public List<Credit> credit { get; set; }
            public int viewcount { get; set; }
            public string description { get; set; }
            public bool premiere { get; set; }
            public int video_year { get; set; }
            public bool @explicit { get; set; }
            public bool has_lyrics { get; set; }
            public string copyright { get; set; }
            public string format { get; set; }
            public string footage_type { get; set; }
            public List<string> video_flags { get; set; }
            public List<Artist> artists { get; set; }
            public List<object> artists_associated { get; set; }
            public List<object> genres { get; set; }
            public List<object> buylinks { get; set; }
            public int viewcount_yesterday { get; set; }
            public int viewcount_lastweek { get; set; }
            public int viewcount_lastmonth { get; set; }
            public int favoritecount { get; set; }
            public string created_at { get; set; }
            public string modified_at { get; set; }
            public string url_safe_title { get; set; }
            public string Key { get; set; }
            public string Value { get; set; }

        }

        public class VideoList
        {
            public bool success { get; set; }
            public string message { get; set; }
            public int offset { get; set; }
            public int status_code { get; set; }
            public int max { get; set; }
            public int total { get; set; }
            public List<Result> result { get; set; }
        }



        public class MainArtist
        {
            public string artistId { get; set; }
            public string artistName { get; set; }
            public string urlSafeArtistName { get; set; }
            public string imageUrl { get; set; }
            public bool enabled { get; set; }
            public List<object> buyLinks { get; set; }
        }

        public class VideoVersion
        {
            public int version { get; set; }
            public int sourceType { get; set; }
            public string id { get; set; }
            public string data { get; set; }
        }

        public class Metadata
        {
            public string keyType { get; set; }
            public int keyIndex { get; set; }
            public string keyValue { get; set; }
        }

        public class Video
        {
            public string isrc { get; set; }
            public string title { get; set; }
            public string urlSafetitle { get; set; }
            public string urlSafeArtistName { get; set; }
            public string imageUrl { get; set; }
            public int contentProviderId { get; set; }
            public object youTubeId { get; set; }
            public object description { get; set; }
            public string deepLinkUrl { get; set; }
            public string shortUrlId { get; set; }
            public bool isMonetizable { get; set; }
            public bool isPremiere { get; set; }
            public bool allowEmbed { get; set; }
            public double duration { get; set; }
            public DateTime releaseDate { get; set; }
            public bool hasLyrics { get; set; }
            public List<object> featuredArtists { get; set; }
            public List<MainArtist> mainArtists { get; set; }
            public List<object> buyLinks { get; set; }
            public List<VideoVersion> videoVersions { get; set; }
            public List<Metadata> metadata { get; set; }
            public string copyrightLine { get; set; }
            public List<string> genres { get; set; }
            public bool isExplicit { get; set; }
            public bool isFacebookWatch { get; set; }
            public bool isCertified { get; set; }
            public string streamType { get; set; }
            public string programTitle { get; set; }
            public string videoType { get; set; }
            public DateTime launchDate { get; set; }
        }

        public class ErrorInfo
        {
            public object ytid { get; set; }
        }

        public class VideoNode
        {
            public Video video { get; set; }
            public ErrorInfo errorInfo { get; set; }
            public bool isApproved { get; set; }
            public bool isMonetizable { get; set; }
            public object statusMessage { get; set; }
            public object statusDetails { get; set; }
            public int statusCode { get; set; }
            public string countryCode { get; set; }
            public string languageCode { get; set; }
        }


        public class Genre
        {
            public bool success { get; set; }
            public string message { get; set; }
            public int status_code { get; set; }
            public List<Result> result { get; set; }
        }

        public class ExternalUrls
        {
            public string Facebook { get; set; }
            public string OfficialWebsite { get; set; }
        }

        public class Buylink
        {
            public string vendor { get; set; }
            public string link_type { get; set; }
            public string image_url { get; set; }
            public string url { get; set; }
        }

        public class ArtistResult
        {
            public string songkick_id { get; set; }
            public bool on_tour { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public string bio { get; set; }
            public string image_url { get; set; }
            public string url_safename { get; set; }
            public string homepage_url { get; set; }
            public ExternalUrls external_urls { get; set; }
            public string twitter_account { get; set; }
            public List<Buylink> buylinks { get; set; }
            public object viewcount { get; set; }
            public int viewcount_yesterday { get; set; }
            public int viewcount_lastweek { get; set; }
            public int viewcount_lastmonth { get; set; }
            public int favoritecount { get; set; }
            public int video_count { get; set; }
            public string created_at { get; set; }
            public string modified_at { get; set; }
        }

        public class ArtistList
        {
            public bool success { get; set; }
            public string message { get; set; }
            public int offset { get; set; }
            public int status_code { get; set; }
            public int max { get; set; }
            public int total { get; set; }
            public List<ArtistResult> result { get; set; }
        }

        public class ArtistVideoResult
        {
            public string isrc { get; set; }
            public string title { get; set; }
            public string image_url { get; set; }
            public int duration_in_seconds { get; set; }
            public List<ArtistsMain> artists_main { get; set; }
            public List<object> artists_featured { get; set; }
            public int viewcount { get; set; }
            public bool premiere { get; set; }
            public bool @explicit { get; set; }
            public string url_safe_title { get; set; }
        }

        public class ArtistVideo
        {
            public bool success { get; set; }
            public string message { get; set; }
            public int offset { get; set; }
            public int status_code { get; set; }
            public int max { get; set; }
            public int total { get; set; }
            public List<ArtistVideoResult> result { get; set; }
        }
    }
}
