using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.LeagueOfLegends.Twitch;
using MediaBrowser.Channels.LeagueOfLegends.Vimeo;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.LeagueOfLegends
{
    public class LeagueOfLegendsChannel : IChannel, IRequiresMediaInfoCallback
    {
        private const int MaxPageSize = 10;

        private readonly ILogger _logger;
        private readonly LolVideoProvider _lolVideoProvider;
        private readonly TwitchService _twitchService;
        private readonly VimeoService _vimeoService;

        public LeagueOfLegendsChannel(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _lolVideoProvider = new LolVideoProvider(httpClient, jsonSerializer, _logger);
            _twitchService = new TwitchService(httpClient, jsonSerializer);
            _vimeoService = new VimeoService(httpClient, jsonSerializer);
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Clip
                },
                MaxPageSize = MaxPageSize,
                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Video
                },
                SupportsSortOrderToggle = false
            };
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var folderId = FolderId.ParseFolderId(query.FolderId);
            switch (folderId.FolderIdType)
            {
                case FolderIdType.None:
                    var limit = query.Limit.GetValueOrDefault(MaxPageSize);
                    var offset = query.StartIndex.GetValueOrDefault(0);
                    return _lolVideoProvider.FindEvents(limit, offset, cancellationToken);
                case FolderIdType.Event:
                    return _lolVideoProvider.FindDays(folderId.EventId, cancellationToken);
                case FolderIdType.Day:
                    return _lolVideoProvider.FindMatches(folderId.EventId, folderId.DayId, cancellationToken);
                case FolderIdType.Game:
                    return _lolVideoProvider.FindGames(folderId.EventId, folderId.DayId, folderId.GameId, cancellationToken);
                default:
                    _logger.Error("Unknown FolderIdType" + folderId.FolderIdType);
                    return Task.FromResult(new ChannelItemResult());
            }
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Backdrop:
                case ImageType.Thumb:
                    {
                        var path = string.Format("{0}.Images.{1}.jpg", GetType().Namespace, type);
                        return Task.FromResult(new DynamicImageResponse
                        {
                            Format = ImageFormat.Jpg,
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
                ImageType.Backdrop,
                ImageType.Thumb
            };
        }

        public string Name
        {
            get { return Plugin.ChannelName; }
        }

        public string Description
        {
            get { return Plugin.ChannelDescription; }
        }

        // Increment as needed to invalidate all caches
        public string DataVersion
        {
            get { return "1"; }
        }

        public string HomePageUrl
        {
            get { return "http://na.lolesports.com/"; }
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            if (Helpers.IsPlaceholderId(id))
            {
                // Default videos when the game doesn't actually exists
                return await RetrieveRickAstleyChannelMediaInfo(cancellationToken);
            }
            return await RetrieveChannelMediaInfoWithId(id, cancellationToken);
        }

        private async Task<IEnumerable<ChannelMediaInfo>> RetrieveRickAstleyChannelMediaInfo(CancellationToken cancellationToken)
        {
            var rickAstleyVideo = await _vimeoService.GetBroadcast("2619976", cancellationToken);
            var info = rickAstleyVideo.Request.Files.H264.Sd;
            return new List<ChannelMediaInfo>
            {
                new ChannelMediaInfo
                {
                    Id = info.Id,
                    Height = info.Height,
                    Path = info.Url,
                    VideoBitrate = info.Bitrate,
                    Width = info.Width
                }
            };
        }

        private async Task<IEnumerable<ChannelMediaInfo>> RetrieveChannelMediaInfoWithId(string id, CancellationToken cancellationToken)
        {
            var twitchVideoId = new TwitchVideoId(id);
            var broadcast = await _twitchService.GetBroadcast(twitchVideoId.Id, cancellationToken);
            return broadcast.Chunks
                .OrderByDescending(pair => pair.Key)
                .First()
                .Value
                .Select(CreateChannelMediaInfo);
        }

        private static ChannelMediaInfo CreateChannelMediaInfo(Chunk chunk)
        {
            long duration = TimeSpan.FromSeconds(chunk.Length).Ticks;
            return new ChannelMediaInfo
            {
                Path = chunk.Url,
                RunTimeTicks = duration
            };
        }
    }
}
