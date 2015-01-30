using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.TouTv.TouTvApi;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.TouTv
{
    public class TouTvChannel : IChannel, IRequiresMediaInfoCallback
    {
        private readonly ILogger _logger;
        private readonly TouTvProvider _touTvProvider;

        public TouTvChannel(ILogManager logManager, IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _logger = logManager.GetLogger(GetType().Name);

            var presentationService = new PresentationService(httpClient, jsonSerializer);
            var mediaValidationV1Service = new MediaValidationV1Service(httpClient, jsonSerializer);
            _touTvProvider = new TouTvProvider(presentationService, mediaValidationV1Service);
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
            var channelItemInfos = await FindChannelItemInfos(folderId, cancellationToken);
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
            get { return "32"; }
        }

        public string HomePageUrl
        {
            get { return "http://ici.tou.tv/"; }
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            return await _touTvProvider.GetEpisode(id, cancellationToken);
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

        private async Task<IEnumerable<ChannelItemInfo>> FindChannelItemInfos(FolderId folderId, CancellationToken cancellationToken)
        {
            switch (folderId.FolderIdType)
            {
                case FolderIdType.Home:
                    return await _touTvProvider.GetTypesOfMedia(cancellationToken);
                case FolderIdType.Section:
                    return await _touTvProvider.GetShows(folderId.Id, cancellationToken);
                case FolderIdType.Show:
                    return await _touTvProvider.GetShowEpisodes(folderId.Id, cancellationToken);
                default:
                    _logger.Error("Unknown FolderIdType" + folderId.FolderIdType);
                    return Enumerable.Empty<ChannelItemInfo>();
            }
        }
    }
}
