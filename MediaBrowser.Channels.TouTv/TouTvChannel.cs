using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.TouTv
{
    public class TouTvChannel : IChannel, ISearchableChannel, IRequiresMediaInfoCallback
    {
        private readonly ILogger _logger;
        private readonly TouTvVideoService _touTvVideoService;
        private readonly TouTvProvider _touTvProvider;

        public TouTvChannel(ILogManager logManager, IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _touTvVideoService = new TouTvVideoService(httpClient, jsonSerializer);
            _touTvProvider = new TouTvProvider();
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Episode
                },
                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Video
                }
            };
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var folderId = FolderId.ParseFolderId(query.FolderId);
            var channelItemInfos = await FindChannelItemInfos(folderId);
            return new ChannelItemResult
            {
                Items = channelItemInfos.ToList()
            };
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Backdrop:
                case ImageType.Thumb:
                    return GetImage(type, "jpg");
                case ImageType.Logo:
                    return GetImage(type, "png");
                default:
                    throw new ArgumentException("Unsupported image type: " + type);
            }
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Backdrop,
                ImageType.Thumb,
                ImageType.Logo
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
            get { return "2"; }
        }

        public string HomePageUrl
        {
            get { return "http://ici.tou.tv/"; }
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public async Task<IEnumerable<ChannelItemInfo>> Search(ChannelSearchInfo searchInfo, CancellationToken cancellationToken)
        {
            return await _touTvProvider.SearchShow(searchInfo.SearchTerm, cancellationToken);
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            var episode = new Episode(id);
            var episodeMetadata = await _touTvProvider.GetEpisode(episode.ShowId, episode.EpisodeId);
            var videoUrl = await _touTvVideoService.GetVideoUrl(episodeMetadata.PID, cancellationToken);
            return new List<ChannelMediaInfo>
            {
                new ChannelMediaInfo
                {
                    Path = videoUrl.Url,
                    RunTimeTicks = episodeMetadata.LengthSpan.Ticks
                }
            };
        }

        private Task<DynamicImageResponse> GetImage(ImageType type, string imageFormat)
        {
            var path = String.Format("{0}.Images.{1}.{2}", GetType().Namespace, type, imageFormat);
            return Task.FromResult(new DynamicImageResponse
            {
                Format = ImageFormat.Jpg,
                HasImage = true,
                Stream = GetType().Assembly.GetManifestResourceStream(path)
            });
        }

        private async Task<IEnumerable<ChannelItemInfo>> FindChannelItemInfos(FolderId folderId)
        {
            switch (folderId.FolderIdType)
            {
                case FolderIdType.Home:
                    return CreateHomeFolders();
                case FolderIdType.Genres:
                    return await _touTvProvider.GetGenres();
                case FolderIdType.Genre:
                    return await _touTvProvider.GetGenreShows(folderId.Id);
                case FolderIdType.Shows:
                    return await _touTvProvider.GetShows();
                case FolderIdType.Show:
                    return await _touTvProvider.GetShowEpisodes(folderId.Id);
                default:
                    _logger.Error("Unknown FolderIdType" + folderId.FolderIdType);
                    return Enumerable.Empty<ChannelItemInfo>();
            }
        }

        private static IEnumerable<ChannelItemInfo> CreateHomeFolders()
        {
            return new List<ChannelItemInfo>
            {
                CreateFolderChannelItemInfo("Émissions", "", FolderId.CreateShowsFolderId()),
                CreateFolderChannelItemInfo("Genres", "", FolderId.CreateGenresFolderId())
            };
        }

        private static ChannelItemInfo CreateFolderChannelItemInfo(string name, string imageUrl, FolderId folderId)
        {
            return new ChannelItemInfo
            {
                FolderType = ChannelFolderType.Container,
                Id = folderId.ToString(),
                ImageUrl = imageUrl,
                Name = name,
                Type = ChannelItemType.Folder
            };
        }
    }
}
