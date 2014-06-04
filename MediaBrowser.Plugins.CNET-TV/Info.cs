using System.Collections.Generic;

namespace MediaBrowser.Plugins.CNETTV
{
    public class Paging
    {
        public int total { get; set; }
        public int limit { get; set; }
        public int offset { get; set; }
    }

    public class Languages
    {
        public List<Datum> data { get; set; }
        public Paging paging { get; set; }
    }

    public class PrimaryTopic
    {
        public string slug { get; set; }
        public string name { get; set; }
        public Languages languages { get; set; }
        public List<object> topicPath { get; set; }
        public string id { get; set; }
        public string typeName { get; set; }
    }

    public class Datum
    {
        public object bitrate { get; set; }
        public string type { get; set; }
        public string format { get; set; }
        public string mpxPublicId { get; set; }
        public string id { get; set; }
        public string typeName { get; set; }
        public string device { get; set; }
        public string canPid { get; set; }
        public string language { get; set; }
        public string slug { get; set; }
        public string name { get; set; }
    }

    public class Files
    {
        public List<Datum> data { get; set; }
    }

    public class Video
    {
        public string id { get; set; }
        public string title { get; set; }
        public string slug { get; set; }
        public string chapters { get; set; }
        public object mpxId { get; set; }
        public string mpxRefId { get; set; }
        public PrimaryTopic primaryTopic { get; set; }
        public Files files { get; set; }
    }

    public class Policies
    {
        public int @default { get; set; }
        public int test { get; set; }
    }

    public class Colors
    {
        public string controlFrameColor { get; set; }
        public string controlBackgroundColor { get; set; }
        public string controlHoverColor { get; set; }
        public string controlSelectedColor { get; set; }
        public string controlColor { get; set; }
        public string frameColor { get; set; }
        public string playProgressColor { get; set; }
        public string scrubberFrameColor { get; set; }
        public string scrubberColor { get; set; }
        public string scrubTrackColor { get; set; }
        public string pageBackgroundColor { get; set; }
    }

    public class Plugins
    {
        public string __invalid_name__1 { get; set; }
        public string __invalid_name__2 { get; set; }
        public string __invalid_name__3 { get; set; }
        public string __invalid_name__4 { get; set; }
        public string __invalid_name__5 { get; set; }
    }

    public class Default
    {
        public string mpx_account { get; set; }
        public string runtimes { get; set; }
        public string playerUrl { get; set; }
        public string layoutUrl { get; set; }
        public string skinUrl { get; set; }
        public Colors colors { get; set; }
        public Plugins plugins { get; set; }
    }

    public class Players
    {
        public Default @default { get; set; }
    }

    public class Tracking
    {
        public string can_partner_id { get; set; }
        public string comscore_id { get; set; }
        public string comscore_home { get; set; }
        public string comscore_news { get; set; }
        public string comscore_reviews { get; set; }
        public string comscore_videos { get; set; }
        public string nielsen_cid { get; set; }
        public string nielsen_vcid { get; set; }
        public string nielsen_vcid_reviews { get; set; }
        public string nielsen_vcid_home { get; set; }
        public string nielsen_vcid_news { get; set; }
        public string nielsen_vcid_how_to { get; set; }
        public string nielsen_vcid_videos { get; set; }
    }

    public class Config
    {
        public Policies policies { get; set; }
        public Players players { get; set; }
        public Tracking tracking { get; set; }
    }

    public class RootObject
    {
        public object video { get; set; }
        public List<Video> videos { get; set; }
        public string relatedItems { get; set; }
        public bool autoplay { get; set; }
        public string timelineContainer { get; set; }
        public bool monitorProgress { get; set; }
        public object refreshAds { get; set; }
        public string timeRange { get; set; }
        public Config config { get; set; }
        public string layoutUrl { get; set; }
        public string skinUrl { get; set; }
        public string runtime { get; set; }
        public string adPathSwf { get; set; }
        public string adPathJS { get; set; }
        public object policy { get; set; }
    }
}
