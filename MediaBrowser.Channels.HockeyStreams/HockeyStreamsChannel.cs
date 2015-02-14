using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.HockeyStreams.Media;
using MediaBrowser.Channels.HockeyStreams.StreamsApi;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.HockeyStreams
{
    public class HockeyStreamsChannel : IChannel, IRequiresMediaInfoCallback
    {
        private readonly MediaProviderFactory _mediaProviderFactory;

        public HockeyStreamsChannel(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost, ILogManager logManager)
        {
            var hockeyStreamsService = new HockeyStreamsService(httpClient, jsonSerializer, applicationHost);
            var logger = logManager.GetLogger(GetType().Name);
            _mediaProviderFactory = new MediaProviderFactory(hockeyStreamsService, logger);
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
            // TODO: set to "1" before commit
            get { return "29"; }
        }

        public string HomePageUrl
        {
            get { return "www.hockeystreams.com"; }
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
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
                },
                DefaultSortFields = new List<ChannelItemSortField>
                {
                    ChannelItemSortField.PremiereDate
                },
                SupportsSortOrderToggle = true
            };
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var folderProvider = _mediaProviderFactory.GetFolderProvider(query.FolderId);
            var channelMediaInfos = await folderProvider.GetFolders(query.FolderId, cancellationToken);

            return new ChannelItemResult
            {
                Items = channelMediaInfos.ToList()
            };
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            var videoProvider = _mediaProviderFactory.GetVideoProvider(id);
            var url = await videoProvider.GetVideoUrl(id, cancellationToken);

            return new List<ChannelMediaInfo>
            {
                new ChannelMediaInfo
                {
                    Path = url
                }
            };
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Backdrop:
                case ImageType.Thumb:
                    return GetImage(type, "gif");
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

        private Task<DynamicImageResponse> GetImage(ImageType type, string imageFormat)
        {
            var path = String.Format("{0}.Images.{1}.{2}", GetType().Namespace, type, imageFormat);
            return Task.FromResult(new DynamicImageResponse
            {
                Format = ImageFormat.Gif,
                HasImage = true,
                Stream = GetType().Assembly.GetManifestResourceStream(path)
            });
        }
    }
}
