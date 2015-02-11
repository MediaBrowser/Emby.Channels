using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.HockeyStreams.Media.VideoProvider;
using MediaBrowser.Channels.HockeyStreams.StreamsApi;
using MediaBrowser.Controller.Channels;

namespace MediaBrowser.Channels.HockeyStreams.Media.FolderProvider
{
    internal class FavoriteCondensedFolderProvider : IFolderProvider
    {
        private const string FolderId = "favoritecondensed-home";

        private readonly StreamsService _baseStreamsService;

        public FavoriteCondensedFolderProvider(StreamsService baseStreamsService)
        {
            _baseStreamsService = baseStreamsService;
        }

        public bool Match(string folderId)
        {
            return folderId == FolderId;
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetFolders(string folderId, CancellationToken cancellationToken)
        {
            var condensed = await _baseStreamsService.GetCondensedGamesForTeam(Plugin.Instance.Configuration.FavoriteTeam, cancellationToken);
            return condensed.Condensed.Select(CreateChannelItemInfo);
        }

        public static string CreateId()
        {
            return FolderId;
        }

        private ChannelItemInfo CreateChannelItemInfo(HighlightsObject highlightsObject)
        {
            var id = CondensedVideoProvider.CreateId(highlightsObject.Id);
            var name = ChannelInfoHelper.FormatFavoriteMatchName(highlightsObject.HomeTeam, highlightsObject.AwayTeam);
            var overview = string.Format("Played on {0}<br>Event: {1}", highlightsObject.Date, highlightsObject.Event);
            var date = ChannelInfoHelper.ParseDate(highlightsObject.Date);

            return ChannelInfoHelper.CreateChannelItemInfo(id, name, overview, date);
        }
    }
}
