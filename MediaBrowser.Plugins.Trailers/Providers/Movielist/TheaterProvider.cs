using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Plugins.Trailers.Providers.Movielist
{
    class TheaterProvider : BaseProvider, IExtraProvider
    {
        public TheaterProvider(ILogger logger) : base(logger)
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
            get { return TrailerType.ComingSoonToDvd; }
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetChannelItems(CancellationToken cancellationToken)
        {
            var nowplaying = await GetChannelItems("http://www.movie-list.com/nowplaying.php", cancellationToken).ConfigureAwait(false);
            var comingsoon = await GetChannelItems("ww.movie-list.com/comingsoon.php", cancellationToken).ConfigureAwait(false);

            return nowplaying.Concat(comingsoon);
        }
    }
}
