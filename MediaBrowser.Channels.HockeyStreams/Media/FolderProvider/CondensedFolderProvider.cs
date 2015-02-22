using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.HockeyStreams.Media.VideoProvider;
using MediaBrowser.Channels.HockeyStreams.StreamsApi;
using MediaBrowser.Controller.Channels;

namespace MediaBrowser.Channels.HockeyStreams.Media.FolderProvider
{
    internal class CondensedFolderProvider : IFolderProvider
    {
        private const string FolderIdStart = "condensed-";

        private readonly StreamsService _baseStreamsService;

        public CondensedFolderProvider(StreamsService baseStreamsService)
        {
            _baseStreamsService = baseStreamsService;
        }

        public bool Match(string folderId)
        {
            return folderId.StartsWith(FolderIdStart);
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetFolders(string folderId, CancellationToken cancellationToken)
        {
            var date = folderId.Substring(FolderIdStart.Length);
            var condensed = await _baseStreamsService.GetCondensedGamesForDate(date, cancellationToken);
            return TransformCondensedToChannelItemInfos(condensed);
        }

        public static string CreateId(string date)
        {
            return FolderIdStart + date;
        }

        private IEnumerable<ChannelItemInfo> TransformCondensedToChannelItemInfos(CondensedResponse condensed)
        {
            if (condensed == null || condensed.Condensed == null)
            {
                return Enumerable.Empty<ChannelItemInfo>();
            }
            return condensed.Condensed.Select(CreateChannelItemInfo);
        }

        private ChannelItemInfo CreateChannelItemInfo(HighlightsObject highlightsObject)
        {
            var id = CondensedVideoProvider.CreateId(highlightsObject.Id);
            var name = ChannelInfoHelper.FormatMatchName(highlightsObject.HomeTeam, highlightsObject.AwayTeam);
            var overview = string.Format("Played on {0}<br>Event: {1}", highlightsObject.Date, highlightsObject.Event);
            var date = ChannelInfoHelper.ParseDate(highlightsObject.Date);

            return ChannelInfoHelper.CreateChannelItemInfo(id, name, overview, date);
        }
    }
}
