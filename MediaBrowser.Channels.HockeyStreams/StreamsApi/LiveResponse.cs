using System.Collections.Generic;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    internal class LiveResponse : BaseStreamsResponse
    {
        public List<LiveSchedule> Schedule { get; set; }
    }
}
