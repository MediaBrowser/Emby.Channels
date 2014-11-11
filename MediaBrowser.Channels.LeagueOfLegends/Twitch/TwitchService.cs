using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.LeagueOfLegends.Twitch
{
    internal class TwitchService : ApiService
    {
        protected override string BaseUrl
        {
            get { return "https://api.twitch.tv/"; }
        }

        public TwitchService(IHttpClient httpClient, IJsonSerializer jsonSerializer)
            : base(httpClient, jsonSerializer)
        { }

        public Task<Broadcast> GetBroadcast(string id, CancellationToken cancellationToken)
        {
            string url = string.Format("/api/videos/a{0}", id);
            return ExecuteRequest<Broadcast>(url, cancellationToken);
        }

        protected override void BeforeExecute(HttpRequestOptions options)
        {
            options.AcceptHeader = "application/vnd.twitchtv.v2+json";
        }
    }
}
