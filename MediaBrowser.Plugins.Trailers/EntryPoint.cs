using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers
{
    public class EntryPoint : IServerEntryPoint
    {
        public List<IExtraProvider> Providers = new List<IExtraProvider>();
        private readonly IApplicationHost _appHost;
        private readonly IHttpClient _httpClient;

        public static EntryPoint Instance;

        public static string UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/30.0.1599.28 Safari/537.36";

        public EntryPoint(IApplicationHost appHost, IHttpClient httpClient)
        {
            _appHost = appHost;
            _httpClient = httpClient;
            Instance = this;
        }

        public void Run()
        {
            Providers = _appHost.GetExports<IExtraProvider>().ToList();
        }

        public async Task<string> GetAndCacheResponse(string url, TimeSpan cacheLength, CancellationToken cancellationToken)
        {
            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,

                // For apple
                UserAgent = UserAgent,
                EnableUnconditionalCache = true,
                CacheLength = cacheLength
            }))
            {
                using (var reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
