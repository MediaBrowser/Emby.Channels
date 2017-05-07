using MediaBrowser.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Logging;

namespace PodCasts.Entities
{
    public class RssFeed : Feed
    {
        protected override string RootElement
        {
            get { return "channel"; }
        }

        protected override string ItemElement
        {
            get { return "item"; }
        }

        public RssFeed(ILogger logger) : base(logger) { }

        public override ChannelItemInfo CreateChannelItemInfo(XElement root, string feedUrl)
        {
            root = root.Element("channel");

            var item = new ChannelItemInfo
            {
                Name = GetValue(root, "title"),
                Overview = GetValue(root, "description"),
                Id = feedUrl,
                Type = ChannelItemType.Folder
            };

            if (!string.IsNullOrWhiteSpace(item.Name))
            {
                _logger.Debug("Found rss channel: {0}", item.Name);

                var imageElement = root.Element("image");
                if (imageElement != null)
                {
                    item.ImageUrl = GetValue(imageElement, "url");
                }
                else
                {
                    var iTunesImageElement = root.Element(XName.Get("image", "http://www.itunes.com/dtds/podcast-1.0.dtd"));
                    if (iTunesImageElement != null)
                    {
                        item.ImageUrl = GetAttribute(iTunesImageElement, "href");
                    }
                }
            }

            return item;
        }

        protected override ChannelItemInfo CreatePodcast(XElement element)
        {
            var overview = GetValue(element, "description");
            var title = GetValue(element, "title");
            var enclosureElement = element.Element("enclosure");
            var link = enclosureElement?.Attribute("url").Value;

            if (string.IsNullOrWhiteSpace(link))
            {
                return null;
            }

            var pubDate = GetDateTime(element, "pubDate");

            string posterUrl = null;

            // itunes podcasts sometimes don't have a summary 
            if (!string.IsNullOrWhiteSpace(overview))
            {
                overview = WebUtility.HtmlDecode(Regex.Replace(overview, @"<(.|\n)*?>", string.Empty));
            }

            long? runtimeTicks = null;

            var itunesDuration = GetValue(element, "duration", "http://www.itunes.com/dtds/podcast-1.0.dtd");
            TimeSpan runtime;
            if (!string.IsNullOrEmpty(itunesDuration) && TimeSpan.TryParse(itunesDuration, out runtime))
            {
                runtimeTicks = runtime.Ticks;
            }

            string rating = null;
            var itunesExplicit = GetValue(element, "explicit", "http://www.itunes.com/dtds/podcast-1.0.dtd");
            if (string.Equals(itunesExplicit, "yes", StringComparison.OrdinalIgnoreCase))
            {
                rating = "R";
            }
            else if (string.Equals(itunesExplicit, "clean", StringComparison.OrdinalIgnoreCase))
            {
                rating = "R";
            }

            var iTunesImageElement = element.Element(XName.Get("image", "http://www.itunes.com/dtds/podcast-1.0.dtd"));
            if (iTunesImageElement != null)
            {
                posterUrl = GetAttribute(iTunesImageElement, "href");
            }
            if (string.IsNullOrWhiteSpace(posterUrl) && !string.IsNullOrWhiteSpace(overview))
            {
                var match = Regex.Match(overview, @"<img src=[\""\']([^\'\""]+)", RegexOptions.IgnoreCase);
                if (match.Groups.Count > 1)
                {
                    // this will get downloaded later if we need it
                    posterUrl = match.Groups[1].Value;
                }
            }

            string container = null;
            string audioCodec = null;
            int? audioBitrate = null;

            if (string.Equals(Path.GetExtension(link), ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                container = "mp3";
                audioCodec = "mp3";
                // Just take a guess to try and encourage direct play
                audioBitrate = 128000;
            }
            else if (string.Equals(Path.GetExtension(link), ".aac", StringComparison.OrdinalIgnoreCase))
            {
                container = "aac";
                audioCodec = "aac";
                // Just take a guess to try and encourage direct play
                audioBitrate = 128000;
            }

            return new ChannelItemInfo
            {
                Name = title,
                Overview = overview,
                ImageUrl = posterUrl,
                Id = link.GetMD5().ToString("N"),
                Type = ChannelItemType.Media,
                ContentType = ChannelMediaContentType.Podcast,
                MediaType = !IsAudioFile(link) ? ChannelMediaType.Video : ChannelMediaType.Audio,

                MediaSources = new List<ChannelMediaInfo>
                    {
                        new ChannelMediaInfo
                        {
                            Path = link,
                            Container = container,
                            AudioCodec = audioCodec,
                            AudioBitrate = audioBitrate
                        }  
                    },

                DateCreated = pubDate,
                PremiereDate = pubDate,

                RunTimeTicks = runtimeTicks,
                OfficialRating = rating
            };
        }
    }
}
