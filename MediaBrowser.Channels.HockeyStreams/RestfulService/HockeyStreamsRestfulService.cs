using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.HockeyStreams.StreamsApi;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.HockeyStreams.RestfulService
{
    public class HockeyStreamsRestfulService : IRestfulService
    {
        private readonly HockeyStreamsService _hockeyStreamsService;

        public HockeyStreamsRestfulService(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
        {
            _hockeyStreamsService = new HockeyStreamsService(httpClient, jsonSerializer, applicationHost);
        }

        public async Task<LoginTokenResponse> Get(LoginToken loginToken)
        {
            try
            {
                return await TryLogin(loginToken);
            }
            catch (ApiException ex)
            {
                return new LoginTokenResponse
                {
                    Message = ex.Message
                };
            }
        }

        private async Task<LoginTokenResponse> TryLogin(LoginToken loginToken)
        {
            var response = await _hockeyStreamsService.Login(loginToken.Username, loginToken.Password, CancellationToken.None);

            return new LoginTokenResponse
            {
                FavoriteTeam = response.FavTeam,
                Token = response.Token
            };
        }
    }
}
