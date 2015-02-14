using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace MediaBrowser.Plugins.Revision3
{
    public class Revision3ListingDownloader
    {

        private ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public Revision3ListingDownloader(ILogger logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public async Task<RootObject> GetEpisodeList(int offset, InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                Url = String.Format("http://revision3.com/api/getEpisodes.json?api_key=0b1faede6785d04b78735b139ddf2910f34ad601&show_id={0}&offset={1}&limit={2}", query.FolderId, offset, query.Limit),
                CancellationToken = cancellationToken,
                // Seeing errors about block length with this enabled
                EnableHttpCompression = false
            };

            using (var json = await _httpClient.SendAsync(options, "GET").ConfigureAwait(false))
            {
                return _jsonSerializer.DeserializeFromStream<RootObject>(json.Content);
            }
        }

        public async Task<RootObject> GetLatestEpisodeList(CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                Url = "http://revision3.com/api/getEpisodes.json?api_key=0b1faede6785d04b78735b139ddf2910f34ad601&grouping=latest",
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
