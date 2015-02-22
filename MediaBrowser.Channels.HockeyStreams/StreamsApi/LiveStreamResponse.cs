using System.Collections.Generic;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    public class LiveStreamResponse : BaseStreamsResponse
    {
        public string Id { get; set; }
        public string Event { get; set; }
        public string HomeTeam { get; set; }
        public string HomeScore { get; set; }
        public string AwayTeam { get; set; }
        public string AwayScore { get; set; }
        public string StartTime { get; set; }
        public string Period { get; set; }
        public string IsHd { get; set; }
        public string FeedType { get; set; }
        public List<Logo> Logos { get; set; }
        public List<StreamInfo> Streams { get; set; }
        public List<StreamInfo> HdStreams { get; set; }
        public List<StreamInfo> SdStreams { get; set; }
        public List<StreamInfo> NonDvr { get; set; }
        public List<StreamInfo> NonDvrSd { get; set; }
        public List<StreamInfo> NonDvrHd { get; set; }
        public List<StreamInfo> TrueLiveSd { get; set; }
        public List<StreamInfo> TrueLiveHd { get; set; }

        public bool IsHdBool
        {
            get { return IsHd == "1"; }
        }
    }
}
