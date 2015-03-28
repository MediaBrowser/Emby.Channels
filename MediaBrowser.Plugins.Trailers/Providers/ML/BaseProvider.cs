using HtmlAgilityPack;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers.Providers.ML
{
    public abstract class BaseProvider : GlobalBaseProvider
    {
        protected string BaseUrl = "http://www.movie-list.com/";

        protected BaseProvider(ILogger logger) : base(logger)
        {
        }

        public abstract TrailerType TrailerType { get; }

        public async Task<IEnumerable<ChannelItemInfo>> GetChannelItems(string url, CancellationToken cancellationToken)
        {
            var list = new List<ChannelItemInfo>();

            var html = await EntryPoint.Instance.GetAndCacheResponse(url, TimeSpan.FromDays(3), cancellationToken)
                        .ConfigureAwait(false);


            // Remove content we should ignore
            var rightIndex = html.IndexOf("one-third last", StringComparison.OrdinalIgnoreCase);
            if (rightIndex != -1)
            {
                html = html.Substring(0, rightIndex);
            }

            rightIndex = html.LastIndexOf("content-box clearfix", StringComparison.OrdinalIgnoreCase);
            if (rightIndex != -1)
            {
                html = html.Substring(rightIndex);
            }

            // looking for HREF='/trailers/automata'
            const string hrefPattern = "href=\"(?<url>.*?)\"";
            var matches = Regex.Matches(html, hrefPattern, RegexOptions.IgnoreCase);

            for (var i = 0; i < matches.Count; i++)
            {
                var trailerUrl = matches[i].Groups["url"].Value;

                if (!string.IsNullOrEmpty(trailerUrl) && trailerUrl.TrimStart('/').StartsWith("trailers/", StringComparison.OrdinalIgnoreCase))
                {
                    trailerUrl = BaseUrl + trailerUrl.TrimStart('/');

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
                        Logger.ErrorException("Error getting trailer info", ex);
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

            var titleElement = document.DocumentNode.SelectSingleNode("//title");
            var name = titleElement == null ? string.Empty : (titleElement.InnerText ?? string.Empty);
            name = name.Replace("Movie Trailer", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("Movie-List.com", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("Movie-List", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("|", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();

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
                TrailerTypes = new List<TrailerType> { type },
                Id = url,
                MediaType = ChannelMediaType.Video,
                Type = ChannelItemType.Media,
                Name = name,
                ImageUrl = string.IsNullOrWhiteSpace(imageSrc) ? null : (BaseUrl + imageSrc.TrimStart('/')),
                MediaSources = GetMediaInfo(linksList, html),
                DateCreated = DateTime.UtcNow
            };

            // For older trailers just rely on core image providers
            if (TrailerType != TrailerType.ComingSoonToTheaters)
            {
                info.ImageUrl = null;
            }

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

            var text = link == null ? null : link.InnerText;

            if (!string.IsNullOrWhiteSpace(text))
            {
                var suffix = text.TrimStart('t');
                int num;

                if (int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out num))
                {
                    return text;
                }
            }

            return null;
        }

        private List<ChannelMediaInfo> GetMediaInfo(IEnumerable<HtmlNode> nodes, string html)
        {
            var links = nodes.Select(i => i.GetAttributeValue("href", ""))
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .ToList();

            var list = new List<ChannelMediaInfo>();

            foreach (var link in links)
            {
                if (ValidContainers.Any(i => link.EndsWith(i, StringComparison.OrdinalIgnoreCase)))
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
                        RequiredHttpHeaders = GetRequiredHttpHeaders(url),
                        SupportsDirectPlay = false
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

                list.Add(new ChannelMediaInfo
                {
                    Container = Path.GetExtension(url).TrimStart('.'),
                    Path = url,
                    Protocol = MediaProtocol.Http,
                    RequiredHttpHeaders = GetRequiredHttpHeaders(url),
                    SupportsDirectPlay = false
                });

                start = index + srch.Length;
                index = html.IndexOf(srch, start, StringComparison.OrdinalIgnoreCase);
            }

            return list
                .Where(i => IsValidDomain(i.Path))
                .Select(SetValues)
                .ToList();
        }
    }
}
