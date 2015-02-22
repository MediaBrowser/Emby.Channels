using System.Collections.Generic;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    public class OnDemandResponse : BaseStreamsResponse
    {
        public List<OnDemandObject> OnDemand { get; set; }
    }
}
