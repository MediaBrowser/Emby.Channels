using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Channels.HockeyStreams.Media.VideoProvider
{
    internal interface IVideoProvider
    {
        bool Match(string id);
        Task<string> GetVideoUrl(string id, CancellationToken cancellationToken);
    }
}
