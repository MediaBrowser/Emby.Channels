using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.HockeyStreams.StreamsApi;

namespace MediaBrowser.Channels.HockeyStreams.Media.VideoProvider
{
    internal class OnDemandVideoProvider : IVideoProvider
    {
        private const string IdStart = "ondemand-";

        private readonly StreamsService _baseStreamsService;

        public OnDemandVideoProvider(StreamsService baseStreamsService)
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
            var onDemandStream = await _baseStreamsService.GetOnDemandStream(streamId, cancellationToken);
            return FindStreamUrl(onDemandStream);
        }

        public static string CreateId(string streamId)
        {
            return IdStart + streamId;
        }

        private static string FindStreamUrl(OnDemandStream onDemandStream)
        {
            if (onDemandStream.HdStreams.Any())
            {
                return GetFirstStreamUrl(onDemandStream.HdStreams);
            }
            return GetFirstStreamUrl(onDemandStream.SdStreams);
        }

        private static string GetFirstStreamUrl(IEnumerable<StreamInfo> streams)
        {
            var streamInfo = streams.First();
            return streamInfo.Src;
        }
    }
}
