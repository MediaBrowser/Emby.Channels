using HtmlAgilityPack;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;

namespace MediaBrowser.Plugins.Trailers.Providers.ML
{
    public abstract class BaseProvider
    {
        private readonly ILogger _logger;

        protected string BaseUrl = "http://www.movie-list.com/";

        protected BaseProvider(ILogger logger)
        {
            _logger = logger;
        }

        public abstract TrailerType TrailerType { get; }

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
                        var info = await GetTrailerFromUrl(trailerUrl, TrailerType, cancellationToken).ConfigureAwait(false);

                        if (info != null)
                        {
                            list.Add(info);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error getting trailer info", ex);
                    }
                }
            }
            return list;
        }

        private async Task<ChannelItemInfo> GetTrailerFromUrl(string url, TrailerType type, CancellationToken cancellationToken)
        {
            var html = await EntryPoint.Instance.GetAndCacheResponse(url, TimeSpan.FromDays(14), cancellationToken)
                        .ConfigureAwait(false);

            var document = new HtmlDocument();
            document.LoadHtml(html);

            var titleElement = document.DocumentNode.SelectSingleNode("//h2");

            var posterElement = document.DocumentNode.SelectSingleNode("//a[@rel='prettyPhoto[posters]']//img");
            if (posterElement == null)
            {
                posterElement = document.DocumentNode.SelectSingleNode("//section[@class='content-box']//img");
            }
            var imageSrc = posterElement == null ? null : posterElement.GetAttributeValue("src", null);

            var links = document.DocumentNode.SelectNodes("//a");
            var linksList = links == null ? new List<HtmlNode>() : links.ToList();

            var info = new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.MovieExtra,
                ExtraType = ExtraType.Trailer,
                TrailerType = type,
                Id = url,
                MediaType = ChannelMediaType.Video,
                Type = ChannelItemType.Media,
                Name = titleElement == null ? null : titleElement.InnerText,
                ImageUrl = string.IsNullOrWhiteSpace(imageSrc) ? null : (BaseUrl + imageSrc.TrimStart('/')),
                MediaSources = GetMediaInfo(linksList, html)
            };

            var metadataElements = document.DocumentNode.SelectNodes("//*[contains(@class,'cast-meta')]");
            if (metadataElements != null)
            {
                foreach (var elem in metadataElements)
                {
                    FillMetadataFromElement(elem, info);
                }
            }

            var imdbId = GetImdbId(linksList);

            if (!string.IsNullOrWhiteSpace(imdbId))
            {
                info.SetProviderId(MetadataProviders.Imdb, imdbId);
            }

            return info;
        }

        private void FillMetadataFromElement(HtmlNode node, ChannelItemInfo info)
        {
            var text = node.InnerText ?? string.Empty;

            if (text.StartsWith("(", StringComparison.OrdinalIgnoreCase) && text.EndsWith(")", StringComparison.OrdinalIgnoreCase))
            {
                //var studio = text.Trim('(').Trim(')');

                //if (!string.IsNullOrWhiteSpace(studio))
                //{
                //    info.Studios.Add(studio);
                //}
            }
            else if (text.StartsWith("Director:", StringComparison.OrdinalIgnoreCase))
            {
                //info.People.AddRange(GetTextFromSubNodes(node, "span").Select(i => new PersonInfo
                //{
                //    Name = i,
                //    Type = PersonType.Director
                //}));
            }
            else if (text.StartsWith("Writer:", StringComparison.OrdinalIgnoreCase))
            {
                //info.People.AddRange(GetTextFromSubNodes(node, "span").Select(i => new PersonInfo
                //{
                //    Name = i,
                //    Type = PersonType.Writer

                //}));
            }
            else if (text.StartsWith("Genre:", StringComparison.OrdinalIgnoreCase))
            {
                //info.Genres.AddRange(GetTextFromSubNodes(node, "span"));
            }
            else if (text.StartsWith("Plot:", StringComparison.OrdinalIgnoreCase))
            {
                //info.Overview = GetTextFromSubNodes(node, "span").FirstOrDefault();
            }
            else if (text.StartsWith("Cast:", StringComparison.OrdinalIgnoreCase))
            {
                //info.People.AddRange(GetTextFromSubNodes(node, "a").Select(i => new PersonInfo
                //{
                //    Name = i,
                //    Type = PersonType.Actor

                //}));
            }
            else
            {
                text = GetTextFromSubNodes(node, "span")
                    .FirstOrDefault() ?? string.Empty;

                DateTime dateAdded;

                if (DateTime.TryParse(text, out dateAdded))
                {
                    info.DateCreated = DateTime.SpecifyKind(dateAdded, DateTimeKind.Utc);
                }
            }
        }

        private IEnumerable<string> GetTextFromSubNodes(HtmlNode node, string tagName)
        {
            var links = node.SelectNodes("//" + tagName);

            if (links != null)
            {
                return links.Select(i => i.InnerText).Where(i => !string.IsNullOrWhiteSpace(i));
            }

            return new List<string>();
        }

        private string GetImdbId(IEnumerable<HtmlNode> nodes)
        {
            var link = nodes.FirstOrDefault(i => i.GetAttributeValue("href", "")
                .IndexOf("imdb.com/title/", StringComparison.OrdinalIgnoreCase) != -1);

            return link == null ? null : link.InnerText;
        }

        private readonly string[] _validContainers = { ".mov", ".mp4", ".m4v" };

        private readonly string[] _validDomains =
        {
            "regentreleasing",
            "movie-list",
            "warnerbros.com",
            "apple.com",
            "variancefilms.com"
        };

        private List<ChannelMediaInfo> GetMediaInfo(IEnumerable<HtmlNode> nodes, string html)
        {
            var links = nodes.Select(i => i.GetAttributeValue("href", ""))
                .ToList();

            var list = new List<ChannelMediaInfo>();

            foreach (var link in links)
            {
                if (_validContainers.Any(i => link.EndsWith(i, StringComparison.OrdinalIgnoreCase)))
                {
                    //_logger.Debug("Found url: " + link);

                    var url = link;

                    if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        url = BaseUrl + url.TrimStart('/');
                    }

                    list.Add(new ChannelMediaInfo
                    {
                        Container = Path.GetExtension(link).TrimStart('.'),
                        Path = url,
                        Protocol = MediaProtocol.Http,
                        VideoCodec = "h264",
                        AudioCodec = "aac",
                        RequiredHttpHeaders = GetRequiredHttpHeaders(url)
                    });
                }
            }

            const string srch = "file: \"";
            var start = 0;
            var index = html.IndexOf(srch, start, StringComparison.OrdinalIgnoreCase);

            while (index != -1)
            {
                var subString = html.Substring(index + srch.Length);
                var endIndex = subString.IndexOf("\"", StringComparison.OrdinalIgnoreCase);

                if (endIndex == -1)
                {
                    break;
                }

                var url = subString.Substring(0, endIndex);

                //_logger.Debug("Found url: " + url);

                int? width = null;
                int? height = null;

                if (url.IndexOf("1080", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    width = 1920;
                    height = 1080;
                }
                else if (url.IndexOf("720", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    width = 1280;
                    height = 720;
                }
                else if (url.IndexOf("480", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    width = 720;
                    height = 480;
                }
                else if (url.IndexOf("360", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    width = 640;
                    height = 360;
                }

                list.Add(new ChannelMediaInfo
                {
                    Container = Path.GetExtension(url).TrimStart('.'),
                    Path = url,
                    Protocol = MediaProtocol.Http,
                    VideoCodec = "h264",
                    AudioCodec = "aac",
                    Width = width,
                    Height = height,
                    RequiredHttpHeaders = GetRequiredHttpHeaders(url)
                });

                start = index + srch.Length;
                index = html.IndexOf(srch, start, StringComparison.OrdinalIgnoreCase);
            }

            return list
                .Where(i => _validDomains.Any(d => i.Path.IndexOf(d, StringComparison.OrdinalIgnoreCase) != -1))
                .ToList();
        }

        private Dictionary<string, string> GetRequiredHttpHeaders(string url)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (url.IndexOf("apple.com", StringComparison.OrdinalIgnoreCase) != -1)
            {
                dict["User-Agent"] = EntryPoint.UserAgent;
            }

            return dict;
        }
    }
}
