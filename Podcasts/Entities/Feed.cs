using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PodCasts.Entities
{
    public abstract class Feed
    {
        protected readonly ILogger _logger;
        protected abstract string RootElement { get; }
        protected abstract string ItemElement { get; }

        public abstract ChannelItemInfo CreateChannelItemInfo(XElement root, string feedUrl);

        public IEnumerable<ChannelItemInfo> Refresh(XDocument document)
        {
            return document.Root.DescendantsAndSelf(RootElement).SelectMany(root =>
            {
                return from p in (from c in root.Elements(ItemElement) select CreatePodcast(c)) where p != null select p;
            });
        }

        protected abstract ChannelItemInfo CreatePodcast(XElement element);

        protected DateTime GetDateTime(XElement element, string name, string namespaceName = null)
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

        protected string GetValue(XElement element, string name, string namespaceName = null)
        {
            return element.Element(GetXName(name, namespaceName))?.Value;
        }

        protected string GetAttribute(XElement element, string name, string namespaceName = null)
        {
            return element.Attribute(GetXName(name, namespaceName))?.Value;
        }

        protected XName GetXName(string name, string namespaceName = null)
        {
            return !string.IsNullOrWhiteSpace(namespaceName) ? XName.Get(name, namespaceName)
                                                             : XName.Get(name);
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

        protected static readonly Dictionary<string, string> AudioFileExtensionsDictionary = AudioFileExtensions.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

        protected Feed(ILogger logger)
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
            // strip off query string
            path = path.Split('?').FirstOrDefault() ?? string.Empty;

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
        protected static string ReplaceRfc822TimeZoneWithOffset(string value)
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