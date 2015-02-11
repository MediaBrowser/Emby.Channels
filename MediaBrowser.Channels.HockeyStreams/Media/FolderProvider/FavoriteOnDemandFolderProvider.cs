using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.HockeyStreams.Media.VideoProvider;
using MediaBrowser.Channels.HockeyStreams.StreamsApi;
using MediaBrowser.Controller.Channels;

namespace MediaBrowser.Channels.HockeyStreams.Media.FolderProvider
{
    internal class FavoriteOnDemandFolderProvider : IFolderProvider
    {
        private const string FolderId = "favoriteondemand-home";

        private readonly StreamsService _baseStreamsService;

        public FavoriteOnDemandFolderProvider(StreamsService baseStreamsService)
        {
            _baseStreamsService = baseStreamsService;
        }

        public bool Match(string folderId)
        {
            return folderId == FolderId;
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetFolders(string folderId, CancellationToken cancellationToken)
        {
            var onDemand = await _baseStreamsService.GetOnDemandForTeam(Plugin.Instance.Configuration.FavoriteTeam, cancellationToken);
            return onDemand.OnDemand.Select(CreateChannelItemInfo);
        }

        public static string CreateId()
        {
            return FolderId;
        }

        private static ChannelItemInfo CreateChannelItemInfo(OnDemandObject onDemandObject)
        {
            var id = OnDemandVideoProvider.CreateId(onDemandObject.Id);
            var name = ChannelInfoHelper.FormatFavoriteMatchName(onDemandObject.HomeTeam, onDemandObject.AwayTeam);
            var overview = string.Format("Played on {0}<br>Event: {1}<br>Feed type: {2}", onDemandObject.Date, onDemandObject.Event, onDemandObject.FeedType);
            var date = ChannelInfoHelper.ParseDate(onDemandObject.Date);

            return ChannelInfoHelper.CreateChannelItemInfo(id, name, overview, date);
        }
    }
}
