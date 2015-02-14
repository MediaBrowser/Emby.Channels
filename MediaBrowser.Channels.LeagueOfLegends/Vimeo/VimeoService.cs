using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.LeagueOfLegends.Vimeo
{
    internal class VimeoService : ApiService
    {
        protected override string BaseUrl
        {
            get { return "http://player.vimeo.com/"; }
        }

        public VimeoService(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
            : base(httpClient, jsonSerializer, applicationHost)
        { }

        public Task<VimeoVideo> GetBroadcast(string id, CancellationToken cancellationToken)
        {
            string url = string.Format("/video/{0}/config?type=moogaloop&referrer=&player_url=player.vimeo.com&v=1.0.0&cdn_url=http://a.vimeocdn.com", id);
            return ExecuteRequest<VimeoVideo>(url, cancellationToken);
        }
    }
}
