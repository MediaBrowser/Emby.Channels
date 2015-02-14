using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.LeagueOfLegends
{
    internal abstract class ApiService
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IApplicationHost _applicationHost;

        protected abstract string BaseUrl { get; }

        private string UserAgent
        {
            get
            {
                var version = _applicationHost.ApplicationVersion.ToString();
                return string.Format("Media Browser/{0} +http://mediabrowser.tv/", version);
            }
        }

        protected ApiService(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _applicationHost = applicationHost;
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
                UserAgent = UserAgent,
            };
            BeforeExecute(httpRequest);
            var result = await _httpClient.Get(httpRequest);
            return _jsonSerializer.DeserializeFromStream<T>(result);
        }
    }
}
