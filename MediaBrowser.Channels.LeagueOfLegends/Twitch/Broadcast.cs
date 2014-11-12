using System.Collections.Generic;

namespace MediaBrowser.Channels.LeagueOfLegends.Twitch
{
    internal class Broadcast
    {
        public IDictionary<string, IEnumerable<Chunk>> Chunks { get; set; }
    }
}
