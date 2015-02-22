using System.Collections.Generic;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    public class HighlightsResponse : BaseStreamsResponse
    {
        public List<HighlightsObject> Highlights { get; set; }
    }
}
