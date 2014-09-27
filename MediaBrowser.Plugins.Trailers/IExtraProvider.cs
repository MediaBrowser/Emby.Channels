using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers
{
    public interface IExtraProvider
    {
        /// <summary>
        /// Gets the type of the content.
        /// </summary>
        /// <value>The type of the content.</value>
        ChannelMediaContentType ContentType { get; }
        /// <summary>
        /// Gets the type of the extra.
        /// </summary>
        /// <value>The type of the extra.</value>
        ExtraType ExtraType { get; }
        /// <summary>
        /// Gets the type of the trailer.
        /// </summary>
        /// <value>The type of the trailer.</value>
        TrailerType TrailerType { get; }
        /// <summary>
        /// Gets the channel items.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;IEnumerable&lt;ChannelItemInfo&gt;&gt;.</returns>
        Task<IEnumerable<ChannelItemInfo>> GetChannelItems(CancellationToken cancellationToken);
    }
}
