using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Channels;

namespace MediaBrowser.Channels.HockeyStreams.Media.FolderProvider
{
    internal class HomeFolderProvider : IFolderProvider
    {
        private static string FavoriteTeam
        {
            get { return Plugin.Instance.Configuration.FavoriteTeam; }
        }

        public bool Match(string folderId)
        {
            return string.IsNullOrEmpty(folderId);
        }

        public Task<IEnumerable<ChannelItemInfo>> GetFolders(string folderId, CancellationToken cancellationToken)
        {
            return Task.FromResult(CreateHomeFolders());
        }

        private IEnumerable<ChannelItemInfo> CreateHomeFolders()
        {
            yield return ChannelInfoHelper.CreateFolder(LiveFolderProvider.CreateId(), Resources.Live, "");

            if (!string.IsNullOrEmpty(FavoriteTeam))
            {
                yield return
                    ChannelInfoHelper.CreateFolder(FavoriteFolderProvider.CreateId(), FavoriteTeam, "");
            }

            yield return ChannelInfoHelper.CreateFolder(DatesFolderProvider.CreateOnDemandId(), Resources.OnDemand, "");
            yield return ChannelInfoHelper.CreateFolder(DatesFolderProvider.CreateCondensedId(), Resources.Condensed, "");
            // Highlights are not working (all or a lot of the urls are placeholders)
            //yield return ChannelInfoHelper.CreateFolder(DatesFolderProvider.CreateHighlightsId(), Resources.Highlights, "");
        }
    }
}
