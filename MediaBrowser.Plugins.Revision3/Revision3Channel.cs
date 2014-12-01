using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace MediaBrowser.Plugins.Revision3
{
    public class Revision3Channel : IChannel, IRequiresMediaInfoCallback, ISupportsLatestMedia
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public Revision3Channel(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            _httpClient = httpClient;
            _logger = logManager.GetLogger(GetType().Name);
            _jsonSerializer = jsonSerializer;
        }

        public string DataVersion
        {
            get
            {
                // Increment as needed to invalidate all caches
                return "6";
            }
        }

        public string Description
        {
            get { return string.Empty; }
        }

        public string HomePageUrl
        {
            get { return "http://revision3.com"; }
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            ChannelItemResult result;

            _logger.Debug("cat ID : " + query.FolderId);

            if (query.FolderId == null)
            {
                result = await GetChannels(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                result = await GetEpisodes(query, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        private async Task<ChannelItemResult> GetChannels(CancellationToken cancellationToken)
        {
            var downloader = new Revision3ChannelDownloader(_logger, _jsonSerializer, _httpClient);
            var channels = await downloader.GetRevision3ChannelList(cancellationToken);

            var shows = channels.shows.Select(i => new ChannelItemInfo
            {
                Type = ChannelItemType.Folder,
                ImageUrl = i.images.logo_200,
                Name = i.name,
                Id = i.id,
                Overview = i.summary
            });

            return new ChannelItemResult
            {
                Items = shows.ToList(),
                TotalRecordCount = channels.total
            };
        }

        private async Task<ChannelItemResult> GetEpisodes(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var offset = query.StartIndex.GetValueOrDefault();
            var downloader = new Revision3ListingDownloader(_logger, _jsonSerializer, _httpClient);
            var videos = await downloader.GetEpisodeList(offset, query, cancellationToken)
                .ConfigureAwait(false);

            var episodes = videos.episodes.Select(i => new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.Clip,
                ImageUrl = !String.IsNullOrEmpty(i.images.medium) ? i.images.medium : "",
                MediaType = ChannelMediaType.Video,
                Name = i.name,
                Type = ChannelItemType.Media,
                Id = i.slug,
                RunTimeTicks = TimeSpan.FromSeconds(i.duration).Ticks,
                DateCreated = !String.IsNullOrEmpty(i.published) ?
                    Convert.ToDateTime(i.published) : DateTime.MinValue,
                Overview = !String.IsNullOrEmpty(i.summary) ? i.summary : "",

            });

            //var orderedEpisodes = OrderItems(episodes.ToList(), query, cancellationToken);

            return new ChannelItemResult
            {
                Items = episodes.ToList(),
                TotalRecordCount = videos.total
            };
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetLatestMedia(ChannelLatestMediaSearch request, CancellationToken cancellationToken)
        {
            var downloader = new Revision3ListingDownloader(_logger, _jsonSerializer, _httpClient);
            var videos = await downloader.GetLatestEpisodeList(cancellationToken)
                .ConfigureAwait(false);

            return videos.episodes.Select(i => new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.Clip,
                ImageUrl = !String.IsNullOrEmpty(i.images.medium) ? i.images.medium : "",
                MediaType = ChannelMediaType.Video,
                Name = i.show.name + " - " + i.name,
                Type = ChannelItemType.Media,
                Id = i.slug,
                RunTimeTicks = TimeSpan.FromSeconds(i.duration).Ticks,
                DateCreated = !String.IsNullOrEmpty(i.published) ?
                    Convert.ToDateTime(i.published) : DateTime.MinValue,
                Overview = !String.IsNullOrEmpty(i.summary) ? i.summary : ""
            }).OrderByDescending(i => i.DateCreated);
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelMediaInfo>();

            using (var stream = await
                        _httpClient.Get("http://revision3.com/api/oembed/?url=http://revision3.com/" + id + "/&format=json", cancellationToken)
                            .ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    var html = await reader.ReadToEndAsync().ConfigureAwait(false);

                    var idNode = Regex.Match(html, "videoId=(?<id>.*)&e", RegexOptions.IgnoreCase);

                    using (var video = await
                        _httpClient.Get(
                            "http://revision3.com/api/flash?video_id=" + idNode.Groups["id"].Value,
                            cancellationToken)
                            .ConfigureAwait(false))
                    {
                        HtmlNode.ElementsFlags.Remove("link");
                        page.Load(video);

                        var options = page.DocumentNode.Descendants("link").Skip(1)
                        .Select(n => new
                        {
                            Text = n.InnerText
                        }).ToList();

                        foreach (var o in options)
                        {
                            var url = o.Text;
                            _logger.Debug(url);
                            //if (url.Contains("mp4"))
                            //{
                            items.Add(new ChannelMediaInfo
                            {
                                Path = url,
                                Protocol = MediaProtocol.Http
                            });
                            //}
                        }
                    }

                    
                    return items;
                }
            }
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Thumb:
                case ImageType.Backdrop:
                case ImageType.Primary:
                    {
                        var path = GetType().Namespace + ".Images." + type.ToString().ToLower() + ".png";

                        return Task.FromResult(new DynamicImageResponse
                        {
                            Format = ImageFormat.Png,
                            HasImage = true,
                            Stream = GetType().Assembly.GetManifestResourceStream(path)
                        });
                    }
                default:
                    throw new ArgumentException("Unsupported image type: " + type);
            }
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Thumb,
                ImageType.Backdrop,
                ImageType.Primary
            };
        }

        public string Name
        {
            get { return "Revision 3"; }
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                MaxPageSize = 25,
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Clip
                },

                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Video
                },

                SupportsSortOrderToggle = true,

                DefaultSortFields = new List<ChannelItemSortField>
                {
                    ChannelItemSortField.DateCreated,
                    ChannelItemSortField.Name,
                    ChannelItemSortField.Runtime
                },
            };
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        internal static string Escape(string text)
        {
            var array = new[]
	            {
		            '[',
		            '\\',
		            '^',
		            '$',
		            '.',
		            '|',
		            '?',
		            '*',
		            '+',
		            '(',
		            ')'
	            };

            var stringBuilder = new StringBuilder();
            var i = 0;
            var length = text.Length;

            while (i < length)
            {
                var character = text[i];

                if (Array.IndexOf(array, character) != -1)
                {
                    stringBuilder.Append("\\" + character.ToString());
                }
                else
                {
                    stringBuilder.Append(character);
                }
                i++;
            }
            return stringBuilder.ToString();
        }

        public IOrderedEnumerable<ChannelItemInfo> OrderItems(List<ChannelItemInfo> items, InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            if (query.SortBy.HasValue)
            {
                if (query.SortDescending)
                {
                    switch (query.SortBy.Value)
                    {
                        case ChannelItemSortField.Runtime:
                            return items.OrderByDescending(i => i.RunTimeTicks ?? 0);
                        case ChannelItemSortField.DateCreated:
                            return items.OrderByDescending(i => i.DateCreated ?? DateTime.MinValue);
                        default:
                            return items.OrderByDescending(i => i.Name);
                    }
                }
                else
                {
                    switch (query.SortBy.Value)
                    {
                        case ChannelItemSortField.Runtime:
                            return items.OrderBy(i => i.RunTimeTicks ?? 0);
                        case ChannelItemSortField.DateCreated:
                            return items.OrderBy(i => i.DateCreated ?? DateTime.MinValue);
                        default:
                            return items.OrderBy(i => i.Name);
                    }
                }
            }

            return items.OrderBy(i => i.Name);
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        
    }
}