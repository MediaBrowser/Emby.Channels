using System.Collections.Generic;

namespace MediaBrowser.Channels.Vineco
{
    public class User
    {
        public int @private { get; set; }
    }

    public class Record2
    {
        public string username { get; set; }
        public int verified { get; set; }
        public List<object> vanityUrls { get; set; }
        public string created { get; set; }
        public string locale { get; set; }
        public object userId { get; set; }
        public User user { get; set; }
        public object postId { get; set; }
        public object likeId { get; set; }
    }

    public class Likes
    {
        public int count { get; set; }
        public List<Record2> records { get; set; }
        public int nextPage { get; set; }
        public int size { get; set; }
        public string anchorStr { get; set; }
        public object previousPage { get; set; }
        public object anchor { get; set; }
    }

    public class Comments
    {
        public int count { get; set; }
        public List<object> records { get; set; }
        public int nextPage { get; set; }
        public int size { get; set; }
        public string anchorStr { get; set; }
        public object previousPage { get; set; }
        public object anchor { get; set; }
    }

    public class Reposts
    {
        public int count { get; set; }
        public List<object> records { get; set; }
        public int nextPage { get; set; }
        public int size { get; set; }
        public string anchorStr { get; set; }
        public object previousPage { get; set; }
        public object anchor { get; set; }
    }

    public class TimeLineRecord
    {
        public int liked { get; set; }
        public string foursquareVenueId { get; set; }
        public object userId { get; set; }
        public int @private { get; set; }
        public Likes likes { get; set; }
        public string thumbnailUrl { get; set; }
        public int explicitContent { get; set; }
        public int myRepostId { get; set; }
        public List<object> vanityUrls { get; set; }
        public int verified { get; set; }
        public string avatarUrl { get; set; }
        public Comments comments { get; set; }
        public List<object> entities { get; set; }
        public string videoLowURL { get; set; }
        public string permalinkUrl { get; set; }
        public string username { get; set; }
        public string description { get; set; }
        public List<object> tags { get; set; }
        public object postId { get; set; }
        public string videoUrl { get; set; }
        public string created { get; set; }
        public string shareUrl { get; set; }
        public string profileBackground { get; set; }
        public int promoted { get; set; }
        public Reposts reposts { get; set; }
        public string venueCategoryId { get; set; }
        public string venueName { get; set; }
        public string venueCategoryShortName { get; set; }
        public string venueCountryCode { get; set; }
        public string venueState { get; set; }
        public string venueAddress { get; set; }
        public string venueCategoryIconUrl { get; set; }
        public string venueCity { get; set; }
    }

    public class TimeLineData
    {
        public int count { get; set; }
        public List<TimeLineRecord> records { get; set; }
        public int nextPage { get; set; }
        public int size { get; set; }
        public string anchorStr { get; set; }
        public object previousPage { get; set; }
        public int anchor { get; set; }
    }

    public class TimelineList
    {
        public string code { get; set; }
        public TimeLineData data { get; set; }
        public bool success { get; set; }
        public string error { get; set; }
    }


    public class ChannelRecord
    {
        public int priority { get; set; }
        public string exploreIconFullUrl { get; set; }
        public string vanityUrl { get; set; }
        public object featuredChannelId { get; set; }
        public string iconFullUrl { get; set; }
        public string retinaIconFullUrl { get; set; }
        public string exploreRetinaIconUrl { get; set; }
        public object channelId { get; set; }
        public int showRecent { get; set; }
        public string editionCode { get; set; }
        public string exploreRetinaIconFullUrl { get; set; }
        public string retinaIconUrl { get; set; }
        public string iconUrl { get; set; }
        public int splashTimelineId { get; set; }
        public string backgroundColor { get; set; }
        public string fontColor { get; set; }
        public string exploreIconUrl { get; set; }
        public int @event { get; set; }
        public string channel { get; set; }
        public string exploreName { get; set; }
    }

    public class ChannelData
    {
        public int count { get; set; }
        public string anchorStr { get; set; }
        public List<ChannelRecord> records { get; set; }
        public object nextPage { get; set; }
        public int anchor { get; set; }
        public object previousPage { get; set; }
        public int size { get; set; }
    }

    public class ChannelList
    {
        public string code { get; set; }
        public ChannelData data { get; set; }
        public bool success { get; set; }
        public string error { get; set; }
    }


    public class PostRecord
    {
        public int liked { get; set; }
        public object foursquareVenueId { get; set; }
        public long userId { get; set; }
        public int @private { get; set; }
        public Likes likes { get; set; }
        public string thumbnailUrl { get; set; }
        public int explicitContent { get; set; }
        public int myRepostId { get; set; }
        public List<string> vanityUrls { get; set; }
        public int verified { get; set; }
        public string avatarUrl { get; set; }
        public Comments comments { get; set; }
        public List<object> entities { get; set; }
        public string videoLowURL { get; set; }
        public string permalinkUrl { get; set; }
        public string username { get; set; }
        public string description { get; set; }
        public List<object> tags { get; set; }
        public long postId { get; set; }
        public string videoUrl { get; set; }
        public string created { get; set; }
        public string shareUrl { get; set; }
        public string profileBackground { get; set; }
        public int promoted { get; set; }
        public Reposts reposts { get; set; }
    }

    public class PostData
    {
        public int count { get; set; }
        public List<PostRecord> records { get; set; }
        public object nextPage { get; set; }
        public int size { get; set; }
        public string anchorStr { get; set; }
        public object previousPage { get; set; }
        public int anchor { get; set; }
    }

    public class PostList
    {
        public string code { get; set; }
        public PostData data { get; set; }
        public bool success { get; set; }
        public string error { get; set; }
    }

}
