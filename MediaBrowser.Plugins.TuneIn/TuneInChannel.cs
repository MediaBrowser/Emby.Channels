using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Schema;
using HtmlAgilityPack;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Plugins.TuneIn
{
    public class TuneInChannel : IChannel, IRequiresMediaInfoCallback, IHasCacheKey
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IApplicationHost _appHost;

        private String partnerid { get; set; }

        public TuneInChannel(IHttpClient httpClient, ILogManager logManager, IApplicationHost appHost)
        {
            _httpClient = httpClient;
            _appHost = appHost;
            _logger = logManager.GetLogger(GetType().Name);

            partnerid = "uD1X52pA";
        }

        public string DataVersion
        {
            get
            {
                // Increment as needed to invalidate all caches
                return "46";
            }
        }

        public string Description
        {
            get { return "Listen to online radio, find streaming music radio and streaming talk radio with TuneIn."; }
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>();

            _logger.Debug("Category ID " + query.FolderId);

            if (string.IsNullOrWhiteSpace(query.FolderId))
            {
                items = await GetMenu("", query, cancellationToken).ConfigureAwait(false);

                if (Plugin.Instance.Configuration.Username != null)
                {
                    items.Add(new ChannelItemInfo
                    {
                        Name = "My Favorites",
                        Id = "preset_",
                        Type = ChannelItemType.Folder,
                        ImageUrl = GetDefaultImages("My Favorites")
                    });
                }
            }
            else
            {
                var channelID = query.FolderId.Split('_');


                if (channelID[0] == "preset")
                {
                    items = await GetPresets(query, cancellationToken);
                }
                else
                {
                    query.FolderId = channelID[1].Replace("&amp;", "&");

                    if (channelID.Count() > 2)
                    {
                        items = await GetMenu(channelID[2], query, cancellationToken).ConfigureAwait(false);
                    }
                    else
                        items = await GetMenu("", query, cancellationToken).ConfigureAwait(false);
                }
            }

            return new ChannelItemResult()
            {
                Items = items
            };
        }

        private async Task<List<ChannelItemInfo>> GetPresets(InternalChannelItemQuery query,
            CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();
            var url = "http://opml.radiotime.com/Browse.ashx?c=presets&formats=mp3,aac&partnerid=" + partnerid + "&serial=" +
                      _appHost.SystemId;

            if (Plugin.Instance.Configuration.Username != null)
            {
                url = url + "&username=" + Plugin.Instance.Configuration.Username;
            }

            using (var site = await _httpClient.Get(url, CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                if (page.DocumentNode != null)
                {
                    var body = page.DocumentNode.SelectSingleNode("//body");

                    if (body.SelectNodes("//outline[@url and @type=\"audio\"]") != null)
                    {
                        foreach (var node in body.SelectNodes("//outline[@url and @type=\"audio\"]"))
                        {
                            items.Add(new ChannelItemInfo
                            {
                                Name = node.Attributes["text"].Value,
                                Id = "stream_" + node.Attributes["url"].Value,
                                Type = ChannelItemType.Media,
                                ContentType = ChannelMediaContentType.Podcast,
                                ImageUrl = node.Attributes["image"] != null ? node.Attributes["image"].Value : null,
                                MediaType = ChannelMediaType.Audio
                            });
                        }
                    }
                    if (body.SelectNodes("//outline[@key=\"shows\"]") != null)
                    {
                        foreach (var node in body.SelectNodes("//outline[@key=\"shows\"]/outline[@url]"))
                        {
                            items.Add(new ChannelItemInfo
                            {
                                Name = node.Attributes["text"].Value,
                                Id = "subcat_" + node.Attributes["url"].Value,
                                Type = ChannelItemType.Folder,
                                ImageUrl = node.Attributes["image"] != null ? node.Attributes["image"].Value : null
                            });
                        }
                    }
                }
            }

            return items.ToList();
        }



        private
            async Task<List<ChannelItemInfo>> GetMenu(String title, InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();
            var url = "http://opml.radiotime.com/Browse.ashx?formats=mp3,aac&partnerid=" + partnerid + "&serial=" +
                      _appHost.SystemId;

            if (Plugin.Instance.Configuration.LatLon != null)
            {
                url = url + "&latlon=" + Plugin.Instance.Configuration.LatLon;
            }

            if (query.FolderId != null) url = query.FolderId.Replace("&amp;", "&");

            using (var site = await _httpClient.Get(url, CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                if (page.DocumentNode != null)
                {
                    var body = page.DocumentNode.SelectSingleNode("//body");

                    if (body.SelectNodes("./outline[@url and not(@type=\"audio\")]") != null)
                    {
                        _logger.Debug("Num 1");

                        if (body.SelectNodes("./outline[@text=\"Stations\"]/outline") != null)
                        {
                            foreach (var node in body.SelectNodes("./outline[@text=\"Stations\"]/outline"))
                            {
                                items.Add(new ChannelItemInfo
                                {
                                    Name = node.Attributes["text"].Value,
                                    Id = "stream_" + node.Attributes["url"].Value,
                                    ImageUrl = node.Attributes["image"] != null ? node.Attributes["image"].Value : "",
                                    Type = ChannelItemType.Media,
                                    ContentType = ChannelMediaContentType.Podcast,
                                    MediaType = ChannelMediaType.Audio

                                });
                            }
                        }

                        foreach (var node in body.SelectNodes("./outline[@url]"))
                        {
                            items.Add(new ChannelItemInfo
                            {
                                Name = node.Attributes["text"].Value,
                                Id = "subcat_" + node.Attributes["url"].Value,
                                Type = ChannelItemType.Folder,
                                ImageUrl = GetDefaultImages(node.Attributes["text"].Value)
                            });
                        }
                    }
                    else if (body.SelectNodes("./outline[@url and @type=\"audio\"]") != null)
                    {
                        _logger.Debug("Num 2");
                        foreach (var node in body.SelectNodes("./outline[@url]"))
                        {
                            items.Add(new ChannelItemInfo
                            {
                                Name = node.Attributes["text"].Value,
                                Id = "stream_" + node.Attributes["url"].Value,
                                Type = ChannelItemType.Media,
                                ContentType = ChannelMediaContentType.Podcast,
                                ImageUrl = node.Attributes["image"].Value,
                                MediaType = ChannelMediaType.Audio
                            });
                        }
                    }
                    else if (body.SelectNodes("./outline[@text and not(@url) and not(@key=\"related\")]") != null && title == "")
                    {
                        _logger.Debug("Num 3");
                        foreach (
                            var node in body.SelectNodes("./outline[@text and not(@url) and not(@key=\"related\")]"))
                        {
                            if (node.Attributes["text"].Value == "No stations or shows available")
                            {
                                throw new Exception("No stations or shows available");
                            }

                            items.Add(new ChannelItemInfo
                            {
                                Name = node.Attributes["text"].Value,
                                Id = "subcat_" + query.FolderId + "_" + node.Attributes["text"].Value,
                                Type = ChannelItemType.Folder,
                            });
                        }
                    }
                    else if (title != "")
                    {
                        _logger.Debug("Num 4");
                        foreach (var node in body.SelectNodes(String.Format("./outline[@text=\"{0}\"]/outline", title)))
                        {
                            var type = node.Attributes["type"].Value;
                            _logger.Debug("Type : " + type);
                            if (type == "audio")
                            {
                                items.Add(new ChannelItemInfo
                                {
                                    Name = node.Attributes["text"].Value,
                                    Id = "stream_" + node.Attributes["url"].Value,
                                    Type = ChannelItemType.Media,
                                    ContentType = ChannelMediaContentType.Podcast,
                                    ImageUrl = node.Attributes["image"].Value,
                                    MediaType = ChannelMediaType.Audio,

                                });
                            }
                            else
                            {
                                var imageNode = node.Attributes["image"];

                                items.Add(new ChannelItemInfo
                                {
                                    Name = node.Attributes["text"].Value,
                                    Id = "subcat_" + node.Attributes["url"].Value,
                                    ImageUrl = imageNode != null ? imageNode.Value : "",
                                    Type = ChannelItemType.Folder,
                                });
                            }
                        }
                    }

                }
            }

            return items.ToList();
        }


        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id,
            CancellationToken cancellationToken)
        {
            var channelID = id.Split('_');
            var items = new List<ChannelMediaInfo>();

            using (var site = await _httpClient.Get(channelID[1] + "&formats=mp3,aac", CancellationToken.None).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(site))
                {
                    while (!reader.EndOfStream)
                    {
                        var url = reader.ReadLine();
                        _logger.Debug("FILE NAME : " + url.Split('/').Last().Split('?').First());

                        var ext = Path.GetExtension(url.Split('/').Last().Split('?').First());

                        _logger.Debug("URL : " + url);
                        if (!string.IsNullOrEmpty(ext))
                        {
                            _logger.Debug("Extension : " + ext);
                            if (ext == ".pls")
                            {
                                try
                                {
                                    using (var value = await _httpClient.Get(url, CancellationToken.None).ConfigureAwait(false))
                                    {
                                        var parser = new IniParser(value);
                                        var count = Convert.ToInt16(parser.GetSetting("playlist", "NumberOfEntries"));
                                        _logger.Debug("COUNT : " + count);
                                        for (var i = 0; i < count; i++)
                                        {
                                            var file = parser.GetSetting("playlist", "File" + count);
                                            _logger.Debug("FILE : " + count + " - " + file);

                                            items.Add(new ChannelMediaInfo
                                            {
                                                Path = file.ToLower()
                                            });
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.Error(ex.ToString());
                                }
                            }
                            else if (ext == ".m3u")
                            {
                                try
                                {
                                    using (var value = await _httpClient.Get(url, CancellationToken.None).ConfigureAwait(false))
                                    {
                                        using (var reader2 = new StreamReader(value))
                                        {
                                            while (!reader2.EndOfStream)
                                            {
                                                var url2 = reader2.ReadLine();
                                                items.Add(new ChannelMediaInfo
                                                {
                                                    Path = url2
                                                });
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.Error(ex.ToString());
                                }
                            }
                            else
                            {
                                items.Add(GetMediaInfoFromUrl(url));
                            }
                        }
                        else
                        {
                            _logger.Debug("Normal URL");

                            items.Add(GetMediaInfoFromUrl(url));
                        }
                    }
                }
            }

            return items;
        }

        private ChannelMediaInfo GetMediaInfoFromUrl(string url)
        {
            var container = url.EndsWith("aac", StringComparison.OrdinalIgnoreCase) ? "aac" : "mp3";

            return new ChannelMediaInfo
            {
                Path = url,
                Container = container,
                AudioCodec = container,
                AudioBitrate = 128000,
                AudioChannels = 2,
                SupportsDirectPlay = true
            };
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
            get { return "TuneIn"; }
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Song
                },
                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Audio
                }
            };
        }

        public string HomePageUrl
        {
            get { return "http://www.tunein.com/"; }
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public string GetCacheKey(string userId)
        {
            return Plugin.Instance.Configuration.LatLon + "-" + Plugin.Instance.Configuration.Username;
        }

        public String GetDefaultImages(String name)
        {
            if (name == "Local Radio")
                return "https://raw.githubusercontent.com/snazy2000/MediaBrowser.Channels/master/MediaBrowser.Plugins.TuneIn/Images/tunein-localradio.png";
            if (name == "By Language")
                return "https://raw.githubusercontent.com/snazy2000/MediaBrowser.Channels/master/MediaBrowser.Plugins.TuneIn/Images/tunein-bylanguage.png";
            if (name == "By Location")
                return "https://raw.githubusercontent.com/snazy2000/MediaBrowser.Channels/master/MediaBrowser.Plugins.TuneIn/Images/tunein-bylocation.png";
            if (name == "Music")
                return "https://raw.githubusercontent.com/snazy2000/MediaBrowser.Channels/master/MediaBrowser.Plugins.TuneIn/Images/tunein-music.png";
            if (name == "My Favorites")
                return "https://raw.githubusercontent.com/snazy2000/MediaBrowser.Channels/master/MediaBrowser.Plugins.TuneIn/Images/tunein-myfavs.png";
            if (name == "Podcasts")
                return "https://raw.githubusercontent.com/snazy2000/MediaBrowser.Channels/master/MediaBrowser.Plugins.TuneIn/Images/tunein-podcasts.png";
            if (name == "Sports")
                return "https://raw.githubusercontent.com/snazy2000/MediaBrowser.Channels/master/MediaBrowser.Plugins.TuneIn/Images/tunein-sports.png";
            if (name == "Talk")
                return "https://raw.githubusercontent.com/snazy2000/MediaBrowser.Channels/master/MediaBrowser.Plugins.TuneIn/Images/tunein-talk.png";

            return "";
        }

    }
}
