using System.Collections.Generic;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    internal class ListTeamsResponse : BaseStreamsResponse
    {
        public List<Team> Teams { get; set; }
    }
}
