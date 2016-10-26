using System;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using MediaBrowser.Plugins.Vimeo.Configuration;

namespace MediaBrowser.Plugins.Vimeo.Api
{
    [Route("/Vimeo/Auth/Request", "GET", Summary = "Gets Token")]
    public class GetToken : IReturn<QueryResult<PluginConfiguration>>
    {
    }

    internal class ServerApiEndpoints : IService
    {

        public object Get(GetToken request)
        {
            Plugin.vc.GetUnauthorizedRequestToken();

            var config = new PluginConfiguration
            {
                TokenURL = Plugin.vc.GenerateAuthorizationUrl(),
                Token = Plugin.vc.GetToken(),
                SecretToken = Plugin.vc.GetSecretToken()
            };

            return config;
        }
    }
}
