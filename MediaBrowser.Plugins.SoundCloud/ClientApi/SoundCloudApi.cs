using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.SoundCloud.ClientApi.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.SoundCloud.ClientApi
{
    public class SoundCloudApi
    {
        public const string ClientId = "78fd88dde7ebf8fdcad08106f6d56ab6";
        public const string ClientSecret = "ef6b3dbe724eff1d03298c2e787a69bd";
        public const string BaseUrl = "https://api.soundcloud.com";

        public const string ClientIdForTracks = "02gUJC0hH2ct1EGOcYXQIzRFU91c72Ea";

        public const bool EnableGZip = true;

        private AccessToken accessToken;

        private ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public SoundCloudApi(ILogger logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        internal bool IsAuthenticated
        {
            get
            {
                return this.accessToken != null;
            }
        }

        public async Task<User> GetMe(CancellationToken cancellationToken)
        {
            var user = await this.Execute<User>("/me", cancellationToken);
            return user;
        }

        public async Task<User> GetUser(int userId, CancellationToken cancellationToken)
        {
            var query = string.Format("/users/{0}", userId);
            var user = await this.Execute<User>(query, cancellationToken);
            return user;
        }

        public async Task<Playlist> GetPlaylist(int playlistId, CancellationToken cancellationToken)
        {
            var query = string.Format("/playlists/{0}", playlistId);

            return await this.Execute<Playlist>(query, cancellationToken, HttpMethod.Get, true, ClientIdForTracks);
        }

        public async Task<ActivityResult> GetActivities(CancellationToken cancellationToken, PagingInfo pagingInfo)
        {
            var result = await this.ExecutePaged<ActivityResult>("/me/activities", pagingInfo, cancellationToken);
            return result;
        }

        public async Task<UserResult> GetFollowings(int userId, CancellationToken cancellationToken, PagingInfo pagingInfo)
        {
            var query = string.Format("/users/{0}/followings", userId);

            var result = await this.ExecutePaged<UserResult>(query, pagingInfo, cancellationToken);
            return result;
        }

        public async Task<UserResult> GetFollowers(int userId, CancellationToken cancellationToken, PagingInfo pagingInfo)
        {
            var query = string.Format("/users/{0}/followers", userId);

            var result = await this.ExecutePaged<UserResult>(query, pagingInfo, cancellationToken);
            return result;
        }

        public async Task<TrackResult> GetUserTracks(int userId, CancellationToken cancellationToken, PagingInfo pagingInfo)
        {
            var query = string.Format("/users/{0}/tracks", userId);

            var result = await this.ExecutePaged<TrackResult>(query, pagingInfo, cancellationToken, ClientIdForTracks);
            return result;

        }

        public async Task<TrackResult> GetLatestTracks(CancellationToken cancellationToken, PagingInfo pagingInfo)
        {
            var query = "/tracks?filter=streamable&order=created_at";

            var result = await this.ExecutePaged<TrackResult>(query, pagingInfo, cancellationToken, ClientIdForTracks);
            return result;

        }

        public async Task<TrackResult> GetFavorites(int userId, CancellationToken cancellationToken, PagingInfo pagingInfo)
        {
            var query = string.Format("/users/{0}/favorites", userId);

            var result = await this.ExecutePaged<TrackResult>(query, pagingInfo, cancellationToken);
            return result;
        }

        public async Task<List<Playlist>> GetPlaylists(int userId, CancellationToken cancellationToken, PagingInfo pagingInfo)
        {
            var query = string.Format("/users/{0}/playlists", userId);

            ////var result = await this.ExecutePaged<PlaylistResult>(query, pagingInfo, cancellationToken);
            var result = await this.Execute<List<Playlist>>(query, cancellationToken);
            return result;
        }

        public async Task<bool> Authenticate(string username, string password, CancellationToken cancellationToken)
        {
            this.accessToken = null;

            var query = string.Format("/oauth2/token?client_id={0}&client_secret={1}&grant_type=password&username={2}&password={3}", ClientId, ClientSecret, username, password);

            var newToken = await this.Execute<AccessToken>(query, cancellationToken, HttpMethod.Post, false);

            if (!string.IsNullOrEmpty(newToken.access_token))
            {
                this.accessToken = newToken;
            }

            return this.accessToken != null;
        }

        private async Task<T> ExecutePaged<T>(string queryString, PagingInfo pagingInfo, CancellationToken cancellationToken, string clientId = null)
            where T : BaseResult
        {
            Uri uri = new Uri(BaseUrl + queryString);
            uri = uri.UriAppendingQueryString("linked_partitioning", "1");
            uri = uri.UriAppendingQueryString("limit", pagingInfo.PageSize.ToString());

            int currentPage = 0;

            var result = await this.Execute<T>(uri.PathAndQuery, cancellationToken, HttpMethod.Get, true, clientId);

            while (currentPage < pagingInfo.PageIndex && !string.IsNullOrEmpty(result.GetNextUrl()))
            {
                currentPage++;
                var nextUri = new Uri(result.GetNextUrl());
                result = await this.Execute<T>(nextUri, cancellationToken);
            }

            if (currentPage == pagingInfo.PageIndex)
            {
                return result;
            }

            return default(T);
        }

        private async Task<T> Execute<T>(string queryString, CancellationToken cancellationToken, HttpMethod method = HttpMethod.Get, bool requireAuthentication = true, string clientId = null)
        {
            Uri uri = new Uri(BaseUrl + queryString);

            if (!string.IsNullOrEmpty(clientId))
            {
                uri = uri.UriWithClientID(clientId);
            }
            else if (requireAuthentication)
            {
                await this.CheckRefreshAccessToken(cancellationToken);

                if (this.accessToken == null)
                {
                    // try an unauthenticated request
                    uri = uri.UriWithClientID(ClientId);
                }
                else
                {
                    uri = uri.UriWithAuthorizedUri(this.accessToken.access_token);
                }
            }

            return await this.Execute<T>(uri, cancellationToken, method);
        }

        private async Task<T> Execute<T>(Uri uri, CancellationToken cancellationToken, HttpMethod method = HttpMethod.Get)
        {
            var options = new HttpRequestOptions
            {
                Url = uri.ToString(),
                CancellationToken = cancellationToken,
                EnableHttpCompression = false
            };

            options.RequestHeaders.Add("Accept-Encoding", "gzip, deflate");

            switch (method)
            {
                case HttpMethod.Get:
                    using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
                    {
                        return this.UnzipAndDeserialize<T>(stream, uri);
                    }

                case HttpMethod.Post:

                    using (var stream = await _httpClient.Post(options, new Dictionary<string, string>()).ConfigureAwait(false))
                    {
                        return this.UnzipAndDeserialize<T>(stream, uri);
                    }
            }

            throw new Exception("Unsupported http method");
        }

        private async Task<bool> CheckRefreshAccessToken(CancellationToken cancellationToken)
        {
            if (this.accessToken == null)
            {
                return false;
            }

            if (!this.accessToken.HasExpired())
            {
                return true;
            }

            var query = string.Format("/oauth2/token?client_id={0}&client_secret={1}&grant_type=refresh_token&refresh_token={2}", ClientId, ClientSecret, this.accessToken.refresh_token);

            this.accessToken = null;
            var newToken = await this.Execute<AccessToken>(query, cancellationToken, HttpMethod.Post, false);

            if (!string.IsNullOrEmpty(newToken.access_token))
            {
                this.accessToken = newToken;
                return true;
            }

            return false;
        }

        private T UnzipAndDeserialize<T>(Stream stream, Uri uri)
        {
            try
            {
                using (var unzipStream = new GZipStream(stream, CompressionMode.Decompress))
                {
                    return _jsonSerializer.DeserializeFromStream<T>(unzipStream);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error deserializing gzipped response from uri: {0}", ex, uri);
            }

            stream.Seek(0, SeekOrigin.Begin);

            return _jsonSerializer.DeserializeFromStream<T>(stream);
        }
    }
}
