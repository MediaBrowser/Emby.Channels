using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.iPlayer
{
    class RSS
    {
        public IEnumerable<ChannelMediaInfo> Children { get; private set; }

        string url;
        SyndicationFeed _feed;

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        
        public RSS(string url, IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            this.url = url;
        }

        public async Task Refresh(CancellationToken cancellationToken)
        {
            try
            {
                using (
                    XmlReader reader =
                        new SyndicationFeedXmlReader(await _httpClient.Get(url, cancellationToken).ConfigureAwait(false))
                    )
                {
                    _feed = SyndicationFeed.Load(reader);

                    Children = await GetChildren(_feed, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.ErrorException("Error loading feed {0}", e, url);
            }
        }

        public string ImageUrl {
            get {
                if (_feed == null || _feed.ImageUrl == null) return null;
                return _feed.ImageUrl.AbsoluteUri;
            }
        }

        public string Title {
            get {
                if (_feed == null) return "";
                return _feed.Title.Text;
            }
        }

        public string Description {
            get {
                if (_feed == null) return null;
                return _feed.Description.Text;
            } 
        } 

        private async Task<IEnumerable<ChannelMediaInfo>> GetChildren(SyndicationFeed feed, CancellationToken cancellationToken) {
            var items = new List<ChannelMediaInfo>();
            
            if (feed == null) return items;

            _logger.Debug("Processing Feed: {0}", feed.Title);

            foreach (var item in feed.Items)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var titleNode = item.Title.ToString().Split(':');
                    var title = item.Title.ToString();
                    if (titleNode.Count() == 3)
                    {
                        title = titleNode[0] + ":" + titleNode[1];
                    }

                    foreach (var link in item.Links)
                    {
                        _logger.Debug("Link Title: " + link.Title);
                        _logger.Debug("URI: " + link.Uri);
                        _logger.Debug("RelationshipType: " + link.RelationshipType);
                        _logger.Debug("MediaType: " + link.MediaType);
                        _logger.Debug("Length: " + link.Length);
                    }

                }
                catch(Exception ex)
                {}
            }

            return items;
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

    }


    /// <summary>
    /// http://stackoverflow.com/questions/210375/problems-reading-rss-with-c-and-net-3-5 workaround datetime issues
    /// </summary>
    public class SyndicationFeedXmlReader : XmlTextReader
    {
        private readonly string[] Rss20DateTimeHints = {"pubDate"};
        private readonly string[] Atom10DateTimeHints = {"updated", "published", "lastBuildDate"};
        private bool isRss2DateTime = false;
        private bool isAtomDateTime = false;

        public SyndicationFeedXmlReader(Stream stream) : base(stream)
        {
        }

        public override bool IsStartElement(string localname, string ns)
        {
            isRss2DateTime = false;
            isAtomDateTime = false;

            if (Rss20DateTimeHints.Contains(localname)) isRss2DateTime = true;
            if (Atom10DateTimeHints.Contains(localname)) isAtomDateTime = true;

            return base.IsStartElement(localname, ns);
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
            return DateTime.TryParseExact(ReplaceRfc822TimeZoneWithOffset(value), formats, dateTimeFormat,
                DateTimeStyles.None, out result);
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

        public override string ReadString()
        {
            string dateVal = base.ReadString();

            try
            {
                if (isRss2DateTime)
                {
                    MethodInfo objMethod = typeof (Rss20FeedFormatter).GetMethod("DateFromString",
                        BindingFlags.NonPublic | BindingFlags.Static);
                    Debug.Assert(objMethod != null);
                    objMethod.Invoke(null, new object[] {dateVal, this});

                }
                if (isAtomDateTime)
                {
                    MethodInfo objMethod = typeof (Atom10FeedFormatter).GetMethod("DateFromString",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    Debug.Assert(objMethod != null);
                    objMethod.Invoke(new Atom10FeedFormatter(), new object[] {dateVal, this});
                }
            }
            catch (TargetInvocationException)
            {
                DateTime date;
                // Microsofts parser bailed 
                if (!TryParseRfc3339DateTime(dateVal, out date) && !TryParseRfc822DateTime(dateVal, out date))
                {
                    date = DateTime.UtcNow;
                }

                DateTimeFormatInfo dtfi = CultureInfo.InvariantCulture.DateTimeFormat;
                dateVal = date.ToString(dtfi.RFC1123Pattern, dtfi);
            }

            return dateVal;

        }
    }

}