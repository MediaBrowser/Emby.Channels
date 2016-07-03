using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.SoundCloud.ClientApi
{
    public static class Helpers
    {
        /// <summary>
        /// Returns a Uri with authorization segment.
        /// </summary>
        /// 
        /// <param name="baseUri">Input Uri.</param>
        /// <param name="token">Token.</param>
        public static Uri UriWithAuthorizedUri(this Uri baseUri, string token)
        {
            return baseUri.UriAppendingQueryString("oauth_token", token);
        }

        /// <summary>
        /// Returns a Uri with authorization segment.
        /// </summary>
        /// <param name="baseUri">Input Uri.</param>
        /// <param name="clientID">The client ID.</param>
        /// <returns></returns>
        public static Uri UriWithClientID(this Uri baseUri, string clientID)
        {
            return baseUri.UriAppendingQueryString("client_id", clientID);
        }

        /// <summary>
        /// Adds query strings to a given uri.
        /// </summary>
        /// 
        /// <param name="baseUri">Input uri.</param>
        /// <param name="parameters">Dictionnary of^parameters to add.</param>
        public static Uri UriAppendingParameters(this Uri baseUri, Dictionary<string, object> parameters)
        {

            var sb = new StringBuilder();

            foreach (KeyValuePair<string, object> pair in parameters)
            {
                sb.AppendFormat("{0}={1}&", pair.Key, pair.Value);
            }

            return baseUri.UriAppendingQueryString(sb.ToString().TrimEnd('&'));
        }
        public static Uri UriAppendingQueryString(this Uri uri, string name, string value)
        {
            return
                new UriBuilder(uri)
                    {
                        Query = (uri.Query + "&" + name + "=" + value).TrimStart('&').TrimStart('?')
                    }
                    .Uri;
        }
        public static Uri UriAppendingQueryString(this Uri uri, string querystring)
        {
            return
                new UriBuilder(uri)
                {
                    Query = (uri.Query + "&" + querystring).TrimStart('&').TrimStart('?')
                }
                .Uri;
        }
    }
}
