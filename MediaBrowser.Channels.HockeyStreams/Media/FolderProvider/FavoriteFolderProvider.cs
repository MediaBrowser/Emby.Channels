using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Channels;

namespace MediaBrowser.Channels.HockeyStreams.Media.FolderProvider
{
    internal class FavoriteFolderProvider : IFolderProvider
    {
        private const string FolderId = "favorite-home";

        public bool Match(string folderId)
        {
            return folderId == FolderId;
        }

        public Task<IEnumerable<ChannelItemInfo>> GetFolders(string folderId, CancellationToken cancellationToken)
        {
            var channelItemInfos = new List<ChannelItemInfo>
            {
                ChannelInfoHelper.CreateFolder(FavoriteOnDemandFolderProvider.CreateId(), Resources.OnDemand, ""),
                ChannelInfoHelper.CreateFolder(FavoriteCondensedFolderProvider.CreateId(), Resources.Condensed, ""),
                // Highlights are not working (all or a lot of the urls are placeholders)
                //ChannelInfoHelper.CreateFolder(FavoriteHighlightsFolderProvider.CreateId(), Resources.OnDemand, ""),
            };

            return Task.FromResult<IEnumerable<ChannelItemInfo>>(channelItemInfos);
        }

        public static string CreateId()
        {
            return FolderId;
        }
    }
}
