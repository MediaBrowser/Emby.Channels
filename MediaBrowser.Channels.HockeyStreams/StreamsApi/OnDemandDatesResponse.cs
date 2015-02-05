using System;
using System.Collections.Generic;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    internal class OnDemandDatesResponse : BaseStreamsResponse
    {
        public List<string> Dates { get; set; }
    }
}
