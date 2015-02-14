using System.Collections.Generic;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    internal class CondensedResponse : BaseStreamsResponse
    {
        public List<HighlightsObject> Condensed { get; set; }
    }
}
