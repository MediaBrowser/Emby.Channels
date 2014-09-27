using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers.Providers.Movielist
{
    public class BaseProvider
    {
        private readonly ILogger _logger;

        public BaseProvider(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetChannelItems(string url, CancellationToken cancellationToken)
        {
            var list = new List<ChannelItemInfo>();

            var html = await EntryPoint.Instance.GetAndCacheResponse(url, TimeSpan.FromDays(7), cancellationToken)
                        .ConfigureAwait(false);


            // Remove content we should ignore
            var rightIndex = html.IndexOf("one-third last", StringComparison.OrdinalIgnoreCase);
            if (rightIndex != -1)
            {
                html = html.Substring(0, rightIndex);
            }

            // looking for HREF='/trailers/automata'
            const string hrefPattern = "href=\"(?<url>.*?)\"";
            var matches = Regex.Matches(html, hrefPattern, RegexOptions.IgnoreCase);

            for (var i = 0; i < matches.Count; i++)
            {
                var trailerUrl = matches[i].Groups["url"].Value;

                if (!string.IsNullOrEmpty(trailerUrl) && trailerUrl.TrimStart('/').StartsWith("trailers/", StringComparison.OrdinalIgnoreCase))
                {
                    trailerUrl = "http://www.movie-list.com/" + trailerUrl.TrimStart('/');

                    try
                    {
                        var info = await GetTrailerFromUrl(trailerUrl, cancellationToken).ConfigureAwait(false);

                        if (info != null)
                        {
                            list.Add(info);
                        }
                    }
                    catch
                    {
                        // logged at lower levels
                    }
                }
            }
            return list;
        }

        private async Task<ChannelItemInfo> GetTrailerFromUrl(string url, CancellationToken cancellationToken)
        {
            var html = await EntryPoint.Instance.GetAndCacheResponse(url, TimeSpan.FromDays(14), cancellationToken)
                        .ConfigureAwait(false);

            // TODO: Get metadata + video urls

            return null;
        }
    }
}
