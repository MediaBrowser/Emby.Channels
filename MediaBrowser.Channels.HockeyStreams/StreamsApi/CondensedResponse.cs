using System.Collections.Generic;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    public class CondensedResponse : BaseStreamsResponse
    {
        public List<HighlightsObject> Condensed { get; set; }
    }
}
