using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    internal class HockeyStreamsService : BaseStreamsService
    {
        protected override string ApiKey
        {
            get { return "d42ccf344acfcfb85b139c10eaa4d339"; }
        }

        public HockeyStreamsService(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
            : base(httpClient, jsonSerializer, applicationHost)
        {
        }

        protected override Task<string> GetBaseUrl(CancellationToken cancellationToken)
        {
            return Task.FromResult("https://api.hockeystreams.com/");
        }
    }
}
