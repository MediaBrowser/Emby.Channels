using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.TouTv.TouTvApi
{
    // This returns HLS streams
    internal class MediaValidationV2Service : ApiService
    {
        private readonly PresentationService _presentationService;

        private static string AccessToken
        {
            get { return Plugin.Instance.Configuration.AccessToken; }
        }

        public MediaValidationV2Service(IHttpClient httpClient, IJsonSerializer jsonSerializer, PresentationService presentationService)
            : base(httpClient, jsonSerializer)
        {
            _presentationService = presentationService;
        }

        protected override async Task<string> GetBaseUrl(CancellationToken cancellationToken)
        {
            var settings = await _presentationService.GetSettings(cancellationToken);
            return settings.SecuredApiBaseHostDefault.TrimStart('/') + "/";
        }

        protected override void BeforeExecute(HttpRequestOptions options)
        {
            var authorization = "Bearer " + AccessToken;
            options.RequestHeaders.Add("Authorization", authorization);
            options.UserAgent = "TouTvApp/2.0.13,(iPad3.1; iOS/8.1.2; fr-ca)";
            options.AcceptHeader = "application/json";
            base.BeforeExecute(options);
        }

        public async Task<string> GetClaims(CancellationToken cancellationToken)
        {
            string url = "/media/validation/v2/GetClaims?token=" + AccessToken;
            var claimsResult = await ExecuteRequest<ClaimsResult>(url, cancellationToken);
            return claimsResult.Claims;
        }

        public async Task<string> GetVideoUrl(string idMedia, string claims, CancellationToken cancellationToken)
        {
            string url = string.Format("/media/validation/v2/?appCode=toutv&deviceType=ipad&connectionType=wifi&idMedia={0}&claims={1}&output=json", idMedia, claims);
            var videoResult = await ExecuteRequest<VideoResult>(url, cancellationToken);
            return videoResult.Url;
        }
    }
}
