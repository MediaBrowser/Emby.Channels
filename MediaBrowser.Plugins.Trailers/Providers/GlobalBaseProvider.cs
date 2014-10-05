using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Plugins.Trailers.Providers
{
    public abstract class GlobalBaseProvider
    {
        protected ILogger Logger;

        protected readonly string[] ValidContainers = { ".mov", ".mp4", ".m4v" };

        protected readonly string[] ValidDomains =
        {
            "regentreleasing",
            "regententertainment.com",
            "movie-list",
            "warnerbros.com",
            "apple.com",
            "variancefilms.com",
            "avideos."
        };

        protected GlobalBaseProvider(ILogger logger)
        {
            Logger = logger;
        }

        protected ChannelMediaInfo SetValues(ChannelMediaInfo info)
        {
            var url = info.Path;

            int? width = null;
            int? height = null;

            // These bitrate numbers are just a guess to try and facilitate direct streaming

            if (url.IndexOf("1080", StringComparison.OrdinalIgnoreCase) != -1)
            {
                width = 1920;
                height = 1080;
                info.VideoBitrate = url.IndexOf("apple", StringComparison.OrdinalIgnoreCase) == -1 ? 3000000 : 11000000;
            }
            else if (url.IndexOf("720", StringComparison.OrdinalIgnoreCase) != -1)
            {
                width = 1280;
                height = 720;
                info.VideoBitrate = 1200000;
            }
            else if (url.IndexOf("480", StringComparison.OrdinalIgnoreCase) != -1)
            {
                width = 720;
                height = 480;
                info.VideoBitrate = 1000000;
            }
            else if (url.IndexOf("360", StringComparison.OrdinalIgnoreCase) != -1)
            {
                width = 640;
                height = 360;
                info.VideoBitrate = 1000000;
            }
            else
            {
                info.VideoBitrate = 3000000;
            }

            info.Height = height;
            info.Width = width;

            info.VideoCodec = "h264";
            info.AudioCodec = "aac";

            info.AudioBitrate = 128000;
            info.AudioChannels = 2;

            return info;
        }

        protected bool IsValidDomain(string url)
        {
            var ok = ValidDomains.Any(d => url.IndexOf(d, StringComparison.OrdinalIgnoreCase) != -1);

            if (!ok)
            {
                Logger.Debug("Ignoring {0}", url);
            }

            return ok;
        }

        protected Dictionary<string, string> GetRequiredHttpHeaders(string url)
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
