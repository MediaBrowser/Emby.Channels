using System;
using System.Collections.Generic;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    public class OnDemandDatesResponse : BaseStreamsResponse
    {
        public List<string> Dates { get; set; }
    }
}
