using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.HockeyStreams.StreamsApi;

namespace MediaBrowser.Channels.HockeyStreams.Media.VideoProvider
{
    internal class CondensedVideoProvider : IVideoProvider
    {
        private const string IdStart = "condensed-";

        private readonly StreamsService _baseStreamsService;

        public CondensedVideoProvider(StreamsService baseStreamsService)
        {
            _baseStreamsService = baseStreamsService;
        }

        public bool Match(string id)
        {
            return id.StartsWith(IdStart);
        }

        public async Task<string> GetVideoUrl(string id, CancellationToken cancellationToken)
        {
            var streamInfo = await GetStreamInfo(id, cancellationToken);
            return ChannelInfoHelper.FirstNotNull(streamInfo.HighQualitySrc, streamInfo.MedQualitySrc, streamInfo.LowQualitySrc);
        }

        public static string CreateId(string id)
        {
            return IdStart + id;
        }

        private async Task<OnDemandStreamInfo> GetStreamInfo(string id, CancellationToken cancellationToken)
        {
            var streamId = id.Substring(IdStart.Length);
            var onDemand = await _baseStreamsService.GetOnDemandStream(streamId, cancellationToken);
            return onDemand.Condensed.First();
        }
    }
}
