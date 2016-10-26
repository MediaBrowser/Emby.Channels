using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.TouTv.TouTvApi;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Channels.TouTv.RestfulService
{
    internal class TouTvRestfulService : IService
    {
        private readonly PresentationService _presentationService;

        public TouTvRestfulService(IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _presentationService = new PresentationService(httpClient, jsonSerializer);
        }

        public async Task<string> Get(LoginUrlBaseToken loginUrlBaseToken)
        {
            var settings = await _presentationService.GetSettings(CancellationToken.None);
            return settings.LoginBaseHostDefault;
        }
    }
}
