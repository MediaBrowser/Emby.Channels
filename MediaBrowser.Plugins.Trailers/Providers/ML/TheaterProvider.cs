using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers.Providers.ML
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

        public override TrailerType TrailerType
        {
            get { return TrailerType.ComingSoonToTheaters; }
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetChannelItems(CancellationToken cancellationToken)
        {
            var nowplaying = await GetChannelItems(BaseUrl + "nowplaying.php", cancellationToken).ConfigureAwait(false);
            var comingsoon = await GetChannelItems(BaseUrl + "comingsoon.php", cancellationToken).ConfigureAwait(false);

            var list = new List<ChannelItemInfo>();

            list.AddRange(nowplaying);
            list.AddRange(comingsoon);

            return list;
        }
    }
}
