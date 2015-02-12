using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    public abstract class ApiService
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IApplicationHost _applicationHost;

        private string UserAgent
        {
            get
            {
                var version = _applicationHost.ApplicationVersion.ToString();
                return string.Format("Media Browser/{0} +http://mediabrowser.tv/", version);
            }
        }

        protected abstract Task<string> GetBaseUrl(CancellationToken cancellationToken);

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

        protected async Task<T> GetRequest<T>(string url, CancellationToken cancellationToken)
            where T : BaseStreamsResponse
        {
            var httpRequest = await PrepareHttpRequestOptions(url, cancellationToken);
            return await TryGetRequest<T>(httpRequest);
        }

        protected async Task<T> PostRequest<T>(string url, IDictionary<string, string> data, CancellationToken cancellationToken)
        {
            var httpRequest = await PrepareHttpRequestOptions(url, cancellationToken);
            httpRequest.SetPostData(data);
            return await TryPostRequest<T>(httpRequest);
        }

        private async Task<HttpRequestOptions> PrepareHttpRequestOptions(string url, CancellationToken cancellationToken)
        {
            var httpRequest = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = await BuildUrl(url, cancellationToken),
                UserAgent = UserAgent
            };
            BeforeExecute(httpRequest);
            return httpRequest;
        }

        private async Task<string> BuildUrl(string url, CancellationToken cancellationToken)
        {
            return await GetBaseUrl(cancellationToken) + url.TrimStart('/');
        }

        private async Task<T> TryGetRequest<T>(HttpRequestOptions httpRequest)
            where T : BaseStreamsResponse
        {
            var resultStream = await _httpClient.Get(httpRequest);
            var response = _jsonSerializer.DeserializeFromStream<T>(resultStream);

            HandleResponseStatus(response);

            return response;
        }

        private static void HandleResponseStatus<T>(T response)
            where T : BaseStreamsResponse
        {
            if (response.Status == Status.Failed)
            {
                if (response.Msg == "Invalid Token")
                {
                    throw new ApiException(Resources.SubscriptionRequired);
                }

                throw new ApiException(response.Msg);
            }
        }

        private async Task<T> TryPostRequest<T>(HttpRequestOptions httpRequest)
        {
            try
            {
                var result = await _httpClient.Post(httpRequest);
                return _jsonSerializer.DeserializeFromStream<T>(result.Content);
            }
            catch (HttpException ex)
            {
                var webException = ex.InnerException as WebException;
                if (webException != null)
                {
                    ThrowExceptionWithMessage(webException);
                }
                throw;
            }
        }

        private void ThrowExceptionWithMessage(WebException webException)
        {
            var stream = webException.Response.GetResponseStream();
            var response = _jsonSerializer.DeserializeFromStream<BaseStreamsResponse>(stream);
            throw new ApiException(response.Msg);
        }
    }
}
