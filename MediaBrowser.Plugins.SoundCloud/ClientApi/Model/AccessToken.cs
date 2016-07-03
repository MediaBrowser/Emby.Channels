using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.SoundCloud.ClientApi.Model
{
    public class AccessToken
    {
        private readonly DateTime createdAt;

        public AccessToken()
        {
            this.createdAt = DateTime.Now;
        }

        public string access_token { get; set; }

        public int expires_in { get; set; }

        public string scope { get; set; }

        public string refresh_token { get; set; }

        public bool HasExpired()
        {
            var tokenExpiresAt = this.createdAt.AddSeconds(expires_in);
            return tokenExpiresAt <= DateTime.Now;
        }
    }
}
