using System.Collections.Generic;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    public class ListTeamsResponse : BaseStreamsResponse
    {
        public List<Team> Teams { get; set; }
    }
}
