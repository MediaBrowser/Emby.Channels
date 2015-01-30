using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.TouTv.TouTvApi
{
    internal abstract class ApiService
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        protected abstract Task<string> GetBaseUrl(CancellationToken cancellationToken);

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
            var httpRequest = await PrepareHttpRequestOptions(url, cancellationToken);
            var result = await _httpClient.Get(httpRequest);
            return _jsonSerializer.DeserializeFromStream<T>(result);
        }

        private async Task<HttpRequestOptions> PrepareHttpRequestOptions(string url, CancellationToken cancellationToken)
        {
            var httpRequest = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = await BuildUrl(url, cancellationToken)
            };
            BeforeExecute(httpRequest);
            return httpRequest;
        }

        private async Task<string> BuildUrl(string url, CancellationToken cancellationToken)
        {
            return await GetBaseUrl(cancellationToken) + url.TrimStart('/');
        }
    }
}
