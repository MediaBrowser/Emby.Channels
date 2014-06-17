using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Channels.iPlayer
{
    public class ListingDownloader
    {

        private ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public ListingDownloader(ILogger logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

       
       
    }
}
