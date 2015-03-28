using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers.Providers.AP
{
    public class TrailerProvider : IExtraProvider
    {
        private readonly ILogger _logger;

        public TrailerProvider(ILogger logger)
        {
            _logger = logger;
        }

        public ChannelMediaContentType ContentType
        {
            get { return ChannelMediaContentType.MovieExtra; }
        }

        public ExtraType ExtraType
        {
            get { return ExtraType.Trailer; }
        }

        public virtual TrailerType TrailerType
        {
            get { return TrailerType.ComingSoonToTheaters; }
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetChannelItems(CancellationToken cancellationToken)
        {
            var hdTrailers = await TrailerListingDownloader.GetTrailerList(_logger,
                true,
                cancellationToken)
                .ConfigureAwait(false);

            var sdTrailers = await TrailerListingDownloader.GetTrailerList(_logger,
                false,
                cancellationToken)
                .ConfigureAwait(false);

            var list = new List<ChannelItemInfo>();

            foreach (var i in hdTrailers)
            {
                // Avoid duplicates
                if (list.Any(l => string.Equals(i.Name, l.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var channelItem = new ChannelItemInfo
                {
                    CommunityRating = i.CommunityRating,
                    ContentType = ChannelMediaContentType.Trailer,
                    Genres = i.Genres,
                    ImageUrl = i.HdImageUrl ?? i.ImageUrl,
                    IsInfiniteStream = false,
                    MediaType = ChannelMediaType.Video,
                    Name = i.Name,
                    OfficialRating = i.OfficialRating,
                    Overview = i.Overview,
                    People = i.People,
                    Type = ChannelItemType.Media,
                    Id = i.TrailerUrl.GetMD5().ToString("N"),
                    PremiereDate = i.PremiereDate,
                    ProductionYear = i.ProductionYear,
                    Studios = i.Studios,
                    RunTimeTicks = i.RunTimeTicks,
                    DateCreated = i.PostDate,

                    MediaSources = new List<ChannelMediaInfo>
                    {
                        GetMediaInfo(i, true)
                    }
                };

                var sdVersion = sdTrailers
                    .FirstOrDefault(l => string.Equals(i.Name, l.Name, StringComparison.OrdinalIgnoreCase));

                if (sdVersion != null)
                {
                    channelItem.MediaSources.Add(GetMediaInfo(sdVersion, false));
                }

                list.Add(channelItem);
            }

            return list;
        }

        private ChannelMediaInfo GetMediaInfo(TrailerInfo info, bool isHd)
        {
            var mediaInfo = new ChannelMediaInfo
            {
                Path = info.TrailerUrl,
                Width = isHd ? 1280 : 720,
                Height = isHd ? 720 : 480,
                Container = (Path.GetExtension(info.TrailerUrl) ?? string.Empty).TrimStart('.'),
                AudioCodec = AudioCodec.AAC,
                VideoCodec = VideoCodec.H264,
                AudioChannels = 2,
                VideoBitrate = isHd ? 11000000 : 1000000,
                AudioBitrate = isHd ? 128000 : 80000,
                AudioSampleRate = 44100,
                Framerate = (float)23.976,
                VideoProfile = isHd ? "high" : "main",
                VideoLevel = isHd ? (float)3.1 : 3,
                SupportsDirectPlay = false
            };

            mediaInfo.RequiredHttpHeaders.Add("User-Agent", "QuickTime/7.7.4");

            return mediaInfo;
        }
    }
}
