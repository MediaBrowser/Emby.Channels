using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.HockeyStreams.StreamsApi;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.HockeyStreams.RestfulService
{
    public class StreamsRestfulService : IRestfulService
    {
        private readonly StreamsService _baseStreamsService;

        public StreamsRestfulService(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
        {
            _baseStreamsService = new StreamsService(httpClient, jsonSerializer, applicationHost);
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
            var response = await _baseStreamsService.Login(loginToken.Username, loginToken.Password, CancellationToken.None);

            return new LoginTokenResponse
            {
                FavoriteTeam = response.FavTeam,
                Token = response.Token
            };
        }
    }
}
