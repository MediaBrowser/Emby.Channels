using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.TouTv
{
    internal class TouTvVideoService
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        private static string UserAgent
        {
            get
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                var assemblyUri = new Uri(assembly.CodeBase);
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assemblyUri.LocalPath);
                string version = fileVersionInfo.FileVersion;
                return string.Format("Media Browser 3/{0} +http://mediabrowser.tv/", version);
            }
        }

        public TouTvVideoService(IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
        }

        public Task<VideoUrl> GetVideoUrl(string id, CancellationToken cancellationToken)
        {
            var url = "http://api.radio-canada.ca/validationMedia/v1/Validation.html?output=json&appCode=thePlatform&deviceType=Android&connectionType=wifi&idMedia=" + id;
            return ExecuteRequest<VideoUrl>(url, cancellationToken);
        }

        private async Task<T> ExecuteRequest<T>(string url, CancellationToken cancellationToken)
        {
            var httpRequest = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                UserAgent = UserAgent,
            };
            var result = await _httpClient.Get(httpRequest);
            return _jsonSerializer.DeserializeFromStream<T>(result);
        }
    }
}
