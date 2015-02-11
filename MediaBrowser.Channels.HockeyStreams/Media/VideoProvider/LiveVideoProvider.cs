using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.HockeyStreams.StreamsApi;

namespace MediaBrowser.Channels.HockeyStreams.Media.VideoProvider
{
    internal class LiveVideoProvider : IVideoProvider
    {
        private const string IdStart = "live-";

        private readonly StreamsService _baseStreamsService;

        public LiveVideoProvider(StreamsService baseStreamsService)
        {
            _baseStreamsService = baseStreamsService;
        }

        public bool Match(string id)
        {
            return id.StartsWith(IdStart);
        }

        public async Task<string> GetVideoUrl(string id, CancellationToken cancellationToken)
        {
            var streamId = id.Substring(IdStart.Length);
            var liveStream = await _baseStreamsService.GetLiveStream(streamId, cancellationToken);
            return FindStreamUrl(liveStream);
        }

        public static string CreateId(string id)
        {
            return IdStart + id;
        }

        private static string FindStreamUrl(LiveStreamResponse liveStream)
        {
            if (liveStream.IsHdBool)
            {
                return GetFirstStreamUrl(liveStream.HdStreams);
            }
            return GetFirstStreamUrl(liveStream.SdStreams);
        }

        private static string GetFirstStreamUrl(IEnumerable<StreamInfo> streams)
        {
            var streamInfo = streams.First();
            return streamInfo.Src;
        }
    }
}
