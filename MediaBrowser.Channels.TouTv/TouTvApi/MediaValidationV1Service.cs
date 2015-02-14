using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.TouTv.TouTvApi
{
    // This one returns RTSP streams
    internal class MediaValidationV1Service : ApiService
    {
        public MediaValidationV1Service(IHttpClient httpClient, IJsonSerializer jsonSerializer)
            : base(httpClient, jsonSerializer)
        { }

        protected override Task<string> GetBaseUrl(CancellationToken cancellationToken)
        {
            return Task.FromResult("http://api.radio-canada.ca/");
        }

        public Task<VideoUrl> GetVideoUrl(string id, CancellationToken cancellationToken)
        {
            var url = "/validationMedia/v1/Validation.html?output=json&appCode=thePlatform&deviceType=Android&connectionType=wifi&idMedia=" + id;
            return ExecuteRequest<VideoUrl>(url, cancellationToken);
        }
    }
}
