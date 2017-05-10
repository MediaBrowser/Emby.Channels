using System;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Logging;
using System.Xml.Linq;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.Channels;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;

namespace PodCasts.Entities
{
    public class AtomFeed : Feed
    {
        protected override string RootElement
        {
            get { return "{http://www.w3.org/2005/Atom}feed"; }
        }

        protected override string ItemElement
        {
            get { return "{http://www.w3.org/2005/Atom}entry"; }
        }

        public AtomFeed(ILogger logger) : base(logger) { }

        public override ChannelItemInfo CreateChannelItemInfo(XElement root, string feedUrl)
        {
            var item = new ChannelItemInfo
            {
                Name = GetValue(root, "title", "http://www.w3.org/2005/Atom"),
                Overview = string.Empty,
                Id = feedUrl,
                Type = ChannelItemType.Folder
            };

            if (!string.IsNullOrWhiteSpace(item.Name))
            {
                _logger.Debug("Found rss channel: {0}", item.Name);
            }

            return item;
        }

        protected override ChannelItemInfo CreatePodcast(XElement element)
        {
            var mediaElement = element.Element("{http://search.yahoo.com/mrss/}group");
            var overview = GetValue(element, "description", "http://search.yahoo.com/mrss/");
            var title = GetValue(element, "{http://www.w3.org/2005/Atom}title");
            var link = element.Elements("{http://www.w3.org/2005/Atom}link").Where(el => (string) el.Attribute("rel") == "alternate").FirstOrDefault()?.Attribute("href").Value;

            if (string.IsNullOrWhiteSpace(link))
            {
                return null;
            }

            var pubDate = GetDateTime(element, "{http://www.w3.org/2005/Atom}published");

            string posterUrl = null;

            // itunes podcasts sometimes don't have a summary 
            if (!string.IsNullOrWhiteSpace(overview))
            {
                overview = WebUtility.HtmlDecode(Regex.Replace(overview, @"<(.|\n)*?>", string.Empty));
            }

            var imageElement = mediaElement?.Element("{http://search.yahoo.com/mrss/}thumbnail");
            if (imageElement != null)
            {
                posterUrl = GetAttribute(imageElement, "url");
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

                RunTimeTicks = null,
                OfficialRating = null
            };
        }
    }
}