using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.LeagueOfLegends
{
    internal abstract class ApiService
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        protected abstract string BaseUrl { get; }

        protected ApiService(IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
        }

        protected virtual void BeforeExecute(HttpRequestOptions options)
        {
            // Do nothing
        }

        protected async Task<T> ExecuteRequest<T>(string url, CancellationToken cancellationToken)
        {
            var httpRequest = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = BaseUrl + url.TrimStart('/'),
                UserAgent = Helpers.UserAgent,
            };
            BeforeExecute(httpRequest);
            var result = await _httpClient.Get(httpRequest);
            return _jsonSerializer.DeserializeFromStream<T>(result);
        }
    }
}
