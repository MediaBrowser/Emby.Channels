using System.Collections.Generic;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    internal class HighlightsResponse : BaseStreamsResponse
    {
        public List<HighlightsObject> Highlights { get; set; }
    }
}
