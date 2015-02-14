using System.Collections.Generic;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    internal class OnDemandResponse : BaseStreamsResponse
    {
        public List<OnDemandObject> OnDemand { get; set; }
    }
}
