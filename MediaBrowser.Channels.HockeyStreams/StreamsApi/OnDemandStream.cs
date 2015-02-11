using System.Collections.Generic;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    public class OnDemandStream : BaseStreamsResponse
    {
        public string Id { get; set; }
        public string Event { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public Logo Logos { get; set; }
        public List<OnDemandStreamInfo> Highlights { get; set; }
        public List<OnDemandStreamInfo> Condensed { get; set; }
        public List<StreamInfo> Streams { get; set; }
        public List<StreamInfo> HdStreams { get; set; }
        public List<StreamInfo> SdStreams { get; set; }
    }
}
