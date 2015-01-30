using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.TouTv.TouTvApi
{
    internal class PresentationService : ApiService
    {
        public PresentationService(IHttpClient httpClient, IJsonSerializer jsonSerializer)
            : base(httpClient, jsonSerializer)
        { }

        protected override Task<string> GetBaseUrl(CancellationToken cancellationToken)
        {
            return Task.FromResult("http://ici.tou.tv/");
        }

        protected override void BeforeExecute(HttpRequestOptions options)
        {
            options.RequestHeaders.Add("ClientId", "95add99f-9445-49f4-aa85-5ef3f77ac032");
            options.UserAgent = "TouTvApp/2.0.13,(iPad3.1; iOS/8.1.2; fr-ca)";
            options.AcceptHeader = "application/json";
            base.BeforeExecute(options);
        }

        public async Task<Settings> GetSettings(CancellationToken cancellationToken)
        {
            return await ExecuteRequest<Settings>("presentation/settings", cancellationToken);
        }

        public async Task<SectionList> GetAlphabeticalSections(CancellationToken cancellationToken)
        {
            return await ExecuteRequest<SectionList>("presentation/section/a-z?AkamaiDevice=tablet&smallWidth=300&mediumWidth=640&largeWidth=980&isPhone=False&includePartnerTeaser=true", cancellationToken);
        }

        public async Task<Section> GetSection(string url, CancellationToken cancellationToken)
        {
            var apiUrl = string.Format("presentation/{0}?excludeLineups=False&AkamaiDevice=tablet&smallWidth=300&mediumWidth=640&largeWidth=980", url);
            return await ExecuteRequest<Section>(apiUrl, cancellationToken);
        }
    }
}
