using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Channels.HockeyStreams.Media.FolderProvider;
using MediaBrowser.Channels.HockeyStreams.Media.VideoProvider;
using MediaBrowser.Channels.HockeyStreams.StreamsApi;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Channels.HockeyStreams.Media
{
    internal class MediaProviderFactory
    {
        private readonly ILogger _logger;
        private readonly IList<IFolderProvider> _folderProviders;
        private readonly IList<IVideoProvider> _videoProviders;

        public MediaProviderFactory(StreamsService baseStreamsService, ILogger logger)
        {
            _logger = logger;

            _folderProviders = new List<IFolderProvider>
            {
                new HomeFolderProvider(),
                new LiveFolderProvider(baseStreamsService),
                new FavoriteFolderProvider(),
                new FavoriteOnDemandFolderProvider(baseStreamsService),
                new FavoriteCondensedFolderProvider(baseStreamsService),
                //new FavoriteHighlightsFolderProvider(baseStreamsService),
                new DatesFolderProvider(baseStreamsService, logger),
                new OnDemandFolderProvider(baseStreamsService),
                new CondensedFolderProvider(baseStreamsService),
                //new HighlightsFolderProvider(baseStreamsService)
            };

            _videoProviders = new List<IVideoProvider>
            {
                new LiveVideoProvider(baseStreamsService),
                new OnDemandVideoProvider(baseStreamsService),
                new CondensedVideoProvider(baseStreamsService),
                //new HighlightsVideoProvider(baseStreamsService)
            };
        }

        public IFolderProvider GetFolderProvider(string folderId)
        {
            var folderProvider = _folderProviders.FirstOrDefault(mp => mp.Match(folderId));

            if (folderProvider == null)
            {
                ThrowException("FolderProvider", folderId);
            }

            return folderProvider;
        }

        public IVideoProvider GetVideoProvider(string id)
        {
            var videoProvider = _videoProviders.FirstOrDefault(mp => mp.Match(id));

            if (videoProvider == null)
            {
                ThrowException("VideoProvider", id);
            }

            return videoProvider;
        }

        private void ThrowException(string type, string id)
        {
            _logger.Error("Can't find a {0} with id \"{1}\"", type, id);
            throw new Exception(Resources.ThisIsABug);
        }
    }
}
