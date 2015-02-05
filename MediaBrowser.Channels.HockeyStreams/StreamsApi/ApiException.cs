using System;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    internal class ApiException : Exception
    {
        public ApiException(string message)
            : base(message)
        { }
    }
}
