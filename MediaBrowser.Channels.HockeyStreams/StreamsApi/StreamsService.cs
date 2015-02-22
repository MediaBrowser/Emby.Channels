using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    public class StreamsService : ApiService
    {
        public StreamsService(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
            : base(httpClient, jsonSerializer, applicationHost)
        { }

        public async Task<LoginResponse> Login(string username, string password, CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, string>
            {
                { "username", username },
                { "password", password },
                { "key", Helper.ApiKey }
            };
            return await PostRequest<LoginResponse>("/Login", data, cancellationToken);
        }

        public async Task<BaseStreamsResponse> UpdateIpException(CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, string>
            {
                { "token", GetToken() }
            };
            return await PostRequest<BaseStreamsResponse>("/IPException", data, cancellationToken);
        }

        public async Task<LiveResponse> GetLive(CancellationToken cancellationToken)
        {
            var url = "/GetLive?token=" + GetToken();
            return await GetRequest<LiveResponse>(url, cancellationToken);
        }

        public async Task<LiveStreamResponse> GetLiveStream(string streamId, CancellationToken cancellationToken)
        {
            var url = string.Format("/GetLiveStream?id={0}&token={1}", streamId, GetToken());
            return await GetRequest<LiveStreamResponse>(url, cancellationToken);
        }

        public async Task<OnDemandDatesResponse> GetOnDemandDates(CancellationToken cancellationToken)
        {
            var url = "/GetOnDemandDates?token=" + GetToken();
            return await GetRequest<OnDemandDatesResponse>(url, cancellationToken);
        }

        public async Task<OnDemandResponse> GetOnDemandLatest(CancellationToken cancellationToken)
        {
            var url = "/GetOnDemand?token=" + GetToken();
            return await GetRequest<OnDemandResponse>(url, cancellationToken);
        }

        public async Task<OnDemandResponse> GetOnDemandForDate(string date, CancellationToken cancellationToken)
        {
            var url = string.Format("/GetOnDemand?date={0}&token={1}", date, GetToken());
            return await GetRequest<OnDemandResponse>(url, cancellationToken);
        }

        public async Task<OnDemandResponse> GetOnDemandForTeam(string team, CancellationToken cancellationToken)
        {
            var url = string.Format("/GetOnDemand?team={0}&token={1}", team, GetToken());
            return await GetRequest<OnDemandResponse>(url, cancellationToken);
        }

        public async Task<OnDemandStream> GetOnDemandStream(string streamId, CancellationToken cancellationToken)
        {
            var url = string.Format("/GetOnDemandStream?id={0}&token={1}", streamId, GetToken());
            return await GetRequest<OnDemandStream>(url, cancellationToken);
        }

        public async Task<HighlightsResponse> GetHighlightsLatest(CancellationToken cancellationToken)
        {
            var url = "/GetHighlights?token=" + GetToken();
            return await GetRequest<HighlightsResponse>(url, cancellationToken);
        }

        public async Task<HighlightsResponse> GetHighlightsForDate(string date, CancellationToken cancellationToken)
        {
            var url = string.Format("/GetHighlights?date={0}&token={1}", date, GetToken());
            return await GetRequest<HighlightsResponse>(url, cancellationToken);
        }

        public async Task<HighlightsResponse> GetHighlightsForTeam(string team, CancellationToken cancellationToken)
        {
            var url = string.Format("/GetHighlights?team={0}&token={1}", team, GetToken());
            return await GetRequest<HighlightsResponse>(url, cancellationToken);
        }

        public async Task<CondensedResponse> GetCondensedGamesLatest(CancellationToken cancellationToken)
        {
            var url = "/GetCondensedGames?token=" + GetToken();
            return await GetRequest<CondensedResponse>(url, cancellationToken);
        }

        public async Task<CondensedResponse> GetCondensedGamesForDate(string date, CancellationToken cancellationToken)
        {
            var url = string.Format("/GetCondensedGames?date={0}&token={1}", date, GetToken());
            return await GetRequest<CondensedResponse>(url, cancellationToken);
        }

        public async Task<CondensedResponse> GetCondensedGamesForTeam(string team, CancellationToken cancellationToken)
        {
            var url = string.Format("/GetCondensedGames?team={0}&token={1}", team, GetToken());
            return await GetRequest<CondensedResponse>(url, cancellationToken);
        }

        public async Task<ListTeamsResponse> ListTeams(CancellationToken cancellationToken)
        {
            var url = "/ListTeams?token=" + GetToken();
            return await GetRequest<ListTeamsResponse>(url, cancellationToken);
        }

        public async Task<ListTeamsResponse> ListTeamsForLeague(string league, CancellationToken cancellationToken)
        {
            var url = string.Format("/ListTeams?league={0}&token={1}", league, GetToken());
            return await GetRequest<ListTeamsResponse>(url, cancellationToken);
        }

        protected override Task<string> GetBaseUrl(CancellationToken cancellationToken)
        {
            return Task.FromResult(Helper.ApiUrl);
        }

        private string GetToken()
        {
            var token = Plugin.Instance.Configuration.Token;
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception(Resources.AuthenticationRequired);
            }
            return token;
        }
    }
}
