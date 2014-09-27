using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers.Providers.ML
{
    class DvdProvider : BaseProvider, IExtraProvider
    {
        public DvdProvider(ILogger logger) : base(logger)
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
            get { return TrailerType.ComingSoonToDvd; }
        }

        public Task<IEnumerable<ChannelItemInfo>> GetChannelItems(CancellationToken cancellationToken)
        {
            return GetChannelItems(BaseUrl + "ondvd.php", cancellationToken);
        }
    }
}
