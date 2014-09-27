using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Plugins.Trailers.Providers.Movielist
{
    public class ArchiveProvider : BaseProvider, IExtraProvider
    {
        public ArchiveProvider(ILogger logger) : base(logger)
        {
        }

        public ChannelMediaContentType ContentType
        {
            get { return ChannelMediaContentType.MovieExtra; }
        }

        public ExtraType ExtraType
        {
            get { return ExtraType.Trailer; }
        }

        public TrailerType TrailerType
        {
            get { return TrailerType.Archive; }
        }

        public Task<IEnumerable<ChannelItemInfo>> GetChannelItems(CancellationToken cancellationToken)
        {
            return GetChannelItems("http://www.movie-list.com/archive.php", cancellationToken);
        }
    }
}
