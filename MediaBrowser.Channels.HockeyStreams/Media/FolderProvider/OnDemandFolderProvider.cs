using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.HockeyStreams.Media.VideoProvider;
using MediaBrowser.Channels.HockeyStreams.StreamsApi;
using MediaBrowser.Controller.Channels;

namespace MediaBrowser.Channels.HockeyStreams.Media.FolderProvider
{
    internal class OnDemandFolderProvider : IFolderProvider
    {
        private const string FolderIdStarts = "ondemand-";

        private readonly StreamsService _baseStreamsService;

        public OnDemandFolderProvider(StreamsService baseStreamsService)
        {
            _baseStreamsService = baseStreamsService;
        }

        public bool Match(string folderId)
        {
            return folderId.StartsWith(FolderIdStarts);
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetFolders(string folderId, CancellationToken cancellationToken)
        {
            var date = folderId.Substring(FolderIdStarts.Length);
            var onDemand = await _baseStreamsService.GetOnDemandForDate(date, cancellationToken);
            return TransformOnDemandToChannelItemInfos(onDemand);
        }

        public static string CreateId(string date)
        {
            return FolderIdStarts + date;
        }

        private IEnumerable<ChannelItemInfo> TransformOnDemandToChannelItemInfos(OnDemandResponse onDemand)
        {
            if (onDemand == null || onDemand.OnDemand == null)
            {
                return Enumerable.Empty<ChannelItemInfo>();
            }
            return onDemand.OnDemand.Select(CreateChannelItemInfo);
        }

        private ChannelItemInfo CreateChannelItemInfo(OnDemandObject onDemandObject)
        {
            var id = OnDemandVideoProvider.CreateId(onDemandObject.Id);
            var name = ChannelInfoHelper.FormatMatchName(onDemandObject.HomeTeam, onDemandObject.AwayTeam);
            var overview = string.Format("Played on {0}<br>Event: {1}<br>Feed type: {2}", onDemandObject.Date, onDemandObject.Event, onDemandObject.FeedType);
            var date = ChannelInfoHelper.ParseDate(onDemandObject.Date);

            return ChannelInfoHelper.CreateChannelItemInfo(id, name, overview, date);
        }
    }
}
