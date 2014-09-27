using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
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
        private readonly IApplicationPaths _appPaths;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        public static EntryPoint Instance;

        public EntryPoint(IApplicationHost appHost, IApplicationPaths appPaths, IHttpClient httpClient, IFileSystem fileSystem)
        {
            _appHost = appHost;
            _appPaths = appPaths;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
            Instance = this;
        }

        public void Run()
        {
            Providers = _appHost.GetExports<IExtraProvider>().ToList();
        }

        public async Task<string> GetAndCacheResponse(string url, TimeSpan cacheLength, CancellationToken cancellationToken)
        {
            var cachePath = Path.Combine(_appPaths.CachePath, "trailerchannel", url.ToLower().GetMD5().ToString("N") + ".txt");

            try
            {
                if (_fileSystem.GetLastWriteTimeUtc(cachePath).Add(cacheLength) > DateTime.UtcNow)
                {
                    using (var stream = _fileSystem.GetFileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read, true))
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            return await reader.ReadToEndAsync().ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {

            }
            catch (DirectoryNotFoundException)
            {

            }

            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,

                // For apple
                UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/30.0.1599.28 Safari/537.36"
            }))
            {
                using (var fileStream = _fileSystem.GetFileStream(cachePath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }

            using (var stream = _fileSystem.GetFileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read, true))
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
