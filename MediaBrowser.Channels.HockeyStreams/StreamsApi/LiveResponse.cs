using System.Collections.Generic;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    public class LiveResponse : BaseStreamsResponse
    {
        public List<LiveSchedule> Schedule { get; set; }
    }
}
