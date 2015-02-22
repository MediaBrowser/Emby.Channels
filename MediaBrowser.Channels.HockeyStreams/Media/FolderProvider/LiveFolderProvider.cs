using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.HockeyStreams.Media.VideoProvider;
using MediaBrowser.Channels.HockeyStreams.StreamsApi;
using MediaBrowser.Controller.Channels;

namespace MediaBrowser.Channels.HockeyStreams.Media.FolderProvider
{
    internal class LiveFolderProvider : IFolderProvider
    {
        private const string FolderId = "live-home";

        private readonly StreamsService _baseStreamsService;

        public LiveFolderProvider(StreamsService baseStreamsService)
        {
            _baseStreamsService = baseStreamsService;
        }

        public bool Match(string folderId)
        {
            return folderId == FolderId;
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetFolders(string folderId, CancellationToken cancellationToken)
        {
            var liveResponse = await _baseStreamsService.GetLive(cancellationToken);

            if (liveResponse.Schedule == null)
            {
                return Enumerable.Empty<ChannelItemInfo>();
            }

            return liveResponse.Schedule.Select(CreateChannelItemInfo);
        }

        public static string CreateId()
        {
            return FolderId;
        }

        private static ChannelItemInfo CreateChannelItemInfo(LiveSchedule liveSchedule)
        {
            var id = LiveVideoProvider.CreateId(liveSchedule.Id);
            var name = ChannelInfoHelper.FormatMatchName(liveSchedule.HomeTeam, liveSchedule.AwayTeam);
            var overview = string.Format("Starts at {0}<br>Event: {1}<br>Feed type: {2}", liveSchedule.StartTime, liveSchedule.Event, liveSchedule.FeedType);

            return ChannelInfoHelper.CreateChannelItemInfo(id, name, overview, DateTime.Today);
        }
    }
}
