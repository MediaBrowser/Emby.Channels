using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Revision3
{
    class Revision3ChannelDownloader
    {
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public Revision3ChannelDownloader(ILogger logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public async Task<RootObject> GetRevision3ChannelList(CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                Url = "http://revision3.com/api/getShows.json?api_key=0b1faede6785d04b78735b139ddf2910f34ad601&grouping=latest",
                CancellationToken = cancellationToken,

                // Seeing errors about block length with this enabled
                EnableHttpCompression = false
            };

            using (var json = await _httpClient.SendAsync(options, "GET").ConfigureAwait(false))
            {
                return _jsonSerializer.DeserializeFromStream<RootObject>(json.Content);
            }
        }

    }
}
