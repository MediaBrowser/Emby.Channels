using HtmlAgilityPack;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.CNETTV
{
    public class Channel : IChannel, IRequiresMediaInfoCallback
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public Channel(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
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
                return "1";
            }
        }

        public string Description
        {
            get { return string.Empty; }
        }

        public string HomePageUrl
        {
            get { return "http://www.cnet.com/"; }
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            //ChannelItemResult result;

            _logger.Debug("cat ID : " + query.FolderId);

            if (query.FolderId == null)
            {
                return await GetMainMenu(cancellationToken).ConfigureAwait(false);
            }

            var catSplit = query.FolderId.Split('_');

            if (catSplit[0] == "subvideo")
            {
                query.FolderId = catSplit[1];
                return await GetVideos(query, cancellationToken).ConfigureAwait(false);
            }

            if (catSplit[0] == "submenu")
            {
                query.FolderId = catSplit[1];
                return await GetSubMenu(query, cancellationToken).ConfigureAwait(false);
            }

            if (catSplit[0] == "showmenu")
            {
                return await GetShowMenu(cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        private async Task<ChannelItemResult> GetMainMenu(CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();

            using (
                var site = await _httpClient.Get("http://www.cnet.com/videos/", CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                if (page.DocumentNode != null)
                {
                    foreach (var node in page.DocumentNode.SelectNodes("//div[@id=\"videonav\"]/ul/li"))
                    {
                        var url = node.SelectSingleNode("./a");
                        var titlenode = node.SelectSingleNode("./a/text()");

                        var title = titlenode.InnerText;

                        if (title.Contains("Featured")) continue;

                        if (title.Contains("New Releases"))
                        {
                            items.Add(new ChannelItemInfo
                            {
                                Name = title,
                                Id = "subvideo_" + url.Attributes["href"].Value.Replace("#", ""),
                                Type = ChannelItemType.Folder,
                            });
                        }
                        else if (title.Contains("Shows"))
                        {
                            items.Add(new ChannelItemInfo
                            {
                                Name = title,
                                Id = "showmenu_" + url.Attributes["href"].Value.Replace("#", ""),
                                Type = ChannelItemType.Folder,
                            });
                        }
                        else
                        {
                            items.Add(new ChannelItemInfo
                            {
                                Name = title,
                                Id = "submenu_" + url.Attributes["href"].Value.Replace("#", ""),
                                Type = ChannelItemType.Folder,
                            });
                        }
                    }
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList(),
                //TotalRecordCount = channels.total
            };
        }

        private async Task<ChannelItemResult> GetSubMenu(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();

            using (
                var site = await _httpClient.Get("http://www.cnet.com/videos/", CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                if (page.DocumentNode != null)
                {
                    foreach (var node in page.DocumentNode.SelectNodes(String.Format("//ul[@id=\"{0}\"]/li/a", query.FolderId)))
                    {
                        items.Add(new ChannelItemInfo
                        {
                            Name = node.InnerText,
                            Id = "subvideo_" + node.Attributes["href"].Value,
                            Type = ChannelItemType.Folder,
                        });
                    }
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList(),
                //TotalRecordCount = channels.total
            };
        }

        private async Task<ChannelItemResult> GetShowMenu(CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();

            using (
                var site = await _httpClient.Get("http://www.cnet.com/videos/", CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                if (page.DocumentNode != null)
                {
                    foreach (var node in page.DocumentNode.SelectNodes("//div[@section=\"carousel\"]//ul/li/a"))
                    {
                        var url = node.Attributes["href"].Value;
                        var title = node.SelectSingleNode(".//h3//text()");
                        var thumb = node.SelectSingleNode(".//img").Attributes["src"].Value;

                        items.Add(new ChannelItemInfo
                        {
                            Name = title.InnerText,
                            ImageUrl = thumb,
                            Id = "subvideo_" + url,
                            Type = ChannelItemType.Folder,
                        });
                    }
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        private async Task<ChannelItemResult> GetVideos(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();

            using (
                var site = await _httpClient.Get("http://www.cnet.com" + query.FolderId, CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                if (page.DocumentNode != null)
                {
                    var firstVideo = page.DocumentNode.SelectSingleNode("//div[@class=\"cnetVideoPlayer\"]");

                    if (firstVideo != null)
                    {
                        _logger.Debug("PASS1");
                        var json =
                            _jsonSerializer.DeserializeFromString<RootObject>(
                                firstVideo.Attributes["data-cnet-video-options"].Value);
                        if (json.videos != null)
                        {
                            _logger.Debug("PASS2");
                            foreach (var v in json.videos)
                            {
                                _logger.Debug("PASS3");

                                _logger.Debug("PASS4");
                                var item = new ChannelItemInfo
                                {
                                    Name = v.title,
                                    Id = "http://www.cnet.com/videos/" + v.slug,
                                    Type = ChannelItemType.Media,
                                    ContentType = ChannelMediaContentType.Clip,
                                    MediaType = ChannelMediaType.Video,
                                    IsInfiniteStream = false
                                };

                                var thumb =
                                    page.DocumentNode.SelectSingleNode("//a[contains(@href,(\"" + v.slug + "\"))]/img");
                                if (thumb != null)
                                {
                                    item.ImageUrl = thumb.Attributes["src"].Value;
                                }

                                items.Add(item);

                            }
                        }
                    }
                    else
                    {
                        foreach (var node in page.DocumentNode.SelectNodes("//li/a[@class=\"imageLinkWrapper\"]"))
                        {
                            var url = node.Attributes["href"].Value;
                            var title = node.SelectSingleNode(".//div[@class=\"headline\"]//text()").InnerText;
                            var thumb = node.SelectSingleNode(".//img").Attributes["src"].Value;

                            var item = new ChannelItemInfo
                            {
                                Name = title,
                                Id = "http://www.cnet.com" + url,
                                ImageUrl = thumb,
                                Type = ChannelItemType.Media,
                                ContentType = ChannelMediaContentType.Clip,
                                MediaType = ChannelMediaType.Video,
                                IsInfiniteStream = false
                            };
                            items.Add(item);
                        }

                    }
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }


        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelMediaInfo>();

            using (
                var site = await _httpClient.Get(id, CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                if (page.DocumentNode != null)
                {
                    var node = page.DocumentNode.SelectSingleNode("//div[@data-cnet-video-options]");

                    if (node != null)
                    {
                        var json = _jsonSerializer.DeserializeFromString<RootObject>(node.Attributes["data-cnet-video-options"].Value);

                        foreach (var v in json.videos[0].files.data)
                        {
                            if (v.type == "HLS_VARIANT_TABLET" && v.format == "M3U" && v.mpxPublicId != "")
                            {
                                items.Add(new ChannelMediaInfo
                                {
                                    Path = String.Format("http://link.theplatform.com/s/kYEXFC/{0}?&mbr=true", v.mpxPublicId)
                                });
                            }
                        }
                    }
                }
            }
            return items;
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Thumb:
                case ImageType.Backdrop:
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
                ImageType.Backdrop
            };
        }

        public string Name
        {
            get { return "CNET-TV"; }
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
