using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Providers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;

namespace PodCasts.Entities
{
    public class RssFeed
    {
        private readonly ILogger _logger;

        public async Task<IEnumerable<ChannelItemInfo>> Refresh(IProviderManager providerManager,
            IHttpClient httpClient,
            string url,
            INotificationManager notificationManager,
            CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,

                // Seeing some deflate stream errors
                EnableHttpCompression = false
            };

            using (Stream stream = await httpClient.Get(options).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    XDocument document = XDocument.Parse(reader.ReadToEnd());
                    var x = from c in document.Root.Element("channel").Elements("item") select c;

                    return x.Select(CreatePodcast).Where(i => i != null);
                }
            }
        }

        private ChannelItemInfo CreatePodcast(XElement element)
        {
            var overview = GetValue(element, "description");
            var title = GetValue(element, "title");
            var enclosureElement = element.Element("enclosure");
            var link = enclosureElement == null ? null : enclosureElement.Attribute("url").Value;

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

            var itunesDuration = GetValue(element, "duration", "itunes");
            TimeSpan runtime;
            if (!string.IsNullOrEmpty(itunesDuration) && TimeSpan.TryParse(itunesDuration, out runtime))
            {
                runtimeTicks = runtime.Ticks;
            }

            string rating = null;
            var itunesExplicit = GetValue(element, "explicit", "itunes");
            if (string.Equals(itunesExplicit, "yes", StringComparison.OrdinalIgnoreCase))
            {
                rating = "R";
            }
            else if (string.Equals(itunesExplicit, "clean", StringComparison.OrdinalIgnoreCase))
            {
                rating = "R";
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
                            Path = link
                        }  
                    },

                DateCreated = pubDate,
                PremiereDate = pubDate,

                RunTimeTicks = runtimeTicks,
                OfficialRating = rating
            };
        }

        private DateTime GetDateTime(XElement element, string name, string namespaceName = null)
        {
            var value = GetValue(element, name, namespaceName);
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.MinValue;
            }

            DateTime date;
            // Microsofts parser bailed 
            if (!TryParseRfc3339DateTime(value, out date) && !TryParseRfc822DateTime(value, out date))
            {
                date = DateTime.UtcNow;
            }

            return date;
        }

        private string GetValue(XElement element, string name, string namespaceName = null)
        {
            if (!string.IsNullOrWhiteSpace(namespaceName))
            {
                var elem = element.Element(XName.Get(name, namespaceName));

                return elem == null ? null : elem.Value;
            }
            else
            {
                var elem = element.Element(name);

                return elem == null ? null : elem.Value;
            }
        }

        /// <summary>
        /// The audio file extensions
        /// </summary>
        public static readonly string[] AudioFileExtensions = new[]
            {
                ".mp3",
                ".flac",
                ".wma",
                ".aac",
                ".acc",
                ".m4a",
                ".m4b",
                ".wav",
                ".ape",
                ".ogg",
                ".oga"

            };

        private static readonly Dictionary<string, string> AudioFileExtensionsDictionary = AudioFileExtensions.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

        public RssFeed(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Determines whether [is audio file] [the specified args].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is audio file] [the specified args]; otherwise, <c>false</c>.</returns>
        public static bool IsAudioFile(string path)
        {
            var extension = Path.GetExtension(path);

            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            return AudioFileExtensionsDictionary.ContainsKey(extension);
        }

        /// <summary>
        /// From Argotic MIT : http://argotic.codeplex.com/releases/view/14436
        /// </summary>
        public static bool TryParseRfc822DateTime(string value, out DateTime result)
        {
            //------------------------------------------------------------
            //	Local members
            //------------------------------------------------------------
            DateTimeFormatInfo dateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat;
            string[] formats = new string[3];

            //------------------------------------------------------------
            //	Define valid RFC-822 formats
            //------------------------------------------------------------
            formats[0] = dateTimeFormat.RFC1123Pattern;
            formats[1] = "ddd',' d MMM yyyy HH:mm:ss zzz";
            formats[2] = "ddd',' dd MMM yyyy HH:mm:ss zzz";

            //------------------------------------------------------------
            //	Validate parameter  
            //------------------------------------------------------------
            if (String.IsNullOrEmpty(value))
            {
                result = DateTime.MinValue;
                return false;
            }

            //------------------------------------------------------------
            //	Perform conversion of RFC-822 formatted date-time string
            //------------------------------------------------------------
            return DateTime.TryParseExact(ReplaceRfc822TimeZoneWithOffset(value), formats, dateTimeFormat, DateTimeStyles.None, out result);
        }


        /// <summary>
        /// From Argotic MIT : http://argotic.codeplex.com/releases/view/14436
        /// </summary>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParseRfc3339DateTime(string value, out DateTime result)
        {
            //------------------------------------------------------------
            //	Local members
            //------------------------------------------------------------
            DateTimeFormatInfo dateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat;
            string[] formats = new string[15];

            //------------------------------------------------------------
            //	Define valid RFC-3339 formats
            //------------------------------------------------------------
            formats[0] = dateTimeFormat.SortableDateTimePattern;
            formats[1] = dateTimeFormat.UniversalSortableDateTimePattern;
            formats[2] = "yyyy'-'MM'-'dd'T'HH:mm:ss'Z'";
            formats[3] = "yyyy'-'MM'-'dd'T'HH:mm:ss.f'Z'";
            formats[4] = "yyyy'-'MM'-'dd'T'HH:mm:ss.ff'Z'";
            formats[5] = "yyyy'-'MM'-'dd'T'HH:mm:ss.fff'Z'";
            formats[6] = "yyyy'-'MM'-'dd'T'HH:mm:ss.ffff'Z'";
            formats[7] = "yyyy'-'MM'-'dd'T'HH:mm:ss.fffff'Z'";
            formats[8] = "yyyy'-'MM'-'dd'T'HH:mm:ss.ffffff'Z'";
            formats[9] = "yyyy'-'MM'-'dd'T'HH:mm:sszzz";
            formats[10] = "yyyy'-'MM'-'dd'T'HH:mm:ss.ffzzz";
            formats[11] = "yyyy'-'MM'-'dd'T'HH:mm:ss.fffzzz";
            formats[12] = "yyyy'-'MM'-'dd'T'HH:mm:ss.ffffzzz";
            formats[13] = "yyyy'-'MM'-'dd'T'HH:mm:ss.fffffzzz";
            formats[14] = "yyyy'-'MM'-'dd'T'HH:mm:ss.ffffffzzz";

            //------------------------------------------------------------
            //	Validate parameter  
            //------------------------------------------------------------
            if (String.IsNullOrEmpty(value))
            {
                result = DateTime.MinValue;
                return false;
            }

            //------------------------------------------------------------
            //	Perform conversion of RFC-3339 formatted date-time string
            //------------------------------------------------------------
            return DateTime.TryParseExact(value, formats, dateTimeFormat, DateTimeStyles.AssumeUniversal, out result);
        }

        /// <summary>
        /// From Argotic MIT : http://argotic.codeplex.com/releases/view/14436
        /// </summary>
        private static string ReplaceRfc822TimeZoneWithOffset(string value)
        {

            //------------------------------------------------------------
            //	Perform conversion
            //------------------------------------------------------------
            value = value.Trim();
            if (value.EndsWith("UT", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}+0:00", value.TrimEnd("UT".ToCharArray()));
            }
            else if (value.EndsWith("UTC", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}+0:00", value.TrimEnd("UTC".ToCharArray()));
            }
            else if (value.EndsWith("EST", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-05:00", value.TrimEnd("EST".ToCharArray()));
            }
            else if (value.EndsWith("EDT", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-04:00", value.TrimEnd("EDT".ToCharArray()));
            }
            else if (value.EndsWith("CST", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-06:00", value.TrimEnd("CST".ToCharArray()));
            }
            else if (value.EndsWith("CDT", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-05:00", value.TrimEnd("CDT".ToCharArray()));
            }
            else if (value.EndsWith("MST", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-07:00", value.TrimEnd("MST".ToCharArray()));
            }
            else if (value.EndsWith("MDT", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-06:00", value.TrimEnd("MDT".ToCharArray()));
            }
            else if (value.EndsWith("PST", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-08:00", value.TrimEnd("PST".ToCharArray()));
            }
            else if (value.EndsWith("PDT", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-07:00", value.TrimEnd("PDT".ToCharArray()));
            }
            else if (value.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}GMT", value.TrimEnd("Z".ToCharArray()));
            }
            else if (value.EndsWith("A", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-01:00", value.TrimEnd("A".ToCharArray()));
            }
            else if (value.EndsWith("M", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-12:00", value.TrimEnd("M".ToCharArray()));
            }
            else if (value.EndsWith("N", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}+01:00", value.TrimEnd("N".ToCharArray()));
            }
            else if (value.EndsWith("Y", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}+12:00", value.TrimEnd("Y".ToCharArray()));
            }
            else
            {
                return value;
            }
        }

    }
}
