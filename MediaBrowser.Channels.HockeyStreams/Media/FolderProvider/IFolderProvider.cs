using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Channels;

namespace MediaBrowser.Channels.HockeyStreams.Media.FolderProvider
{
    internal interface IFolderProvider
    {
        bool Match(string folderId);
        Task<IEnumerable<ChannelItemInfo>> GetFolders(string folderId, CancellationToken cancellationToken);
    }
}
