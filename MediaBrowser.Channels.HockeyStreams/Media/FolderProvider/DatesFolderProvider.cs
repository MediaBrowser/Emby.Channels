using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.HockeyStreams.StreamsApi;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Channels.HockeyStreams.Media.FolderProvider
{
    internal class DatesFolderProvider : IFolderProvider
    {
        private const string FolderIdOnDemand = "dates-ondemand";
        private const string FolderIdCondensed = "dates-condensed";
        private const string FolderIdHighlights = "dates-highlights";

        private readonly StreamsService _baseStreamsService;
        private readonly ILogger _logger;

        public DatesFolderProvider(StreamsService baseStreamsService, ILogger logger)
        {
            _baseStreamsService = baseStreamsService;
            _logger = logger;
        }

        public bool Match(string folderId)
        {
            return folderId == FolderIdOnDemand || folderId == FolderIdCondensed || folderId == FolderIdHighlights;
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetFolders(string folderId, CancellationToken cancellationToken)
        {
            var dates = await _baseStreamsService.GetOnDemandDates(cancellationToken);
            return dates.Dates.Select(date =>
            {
                var id = GetChildFolderId(folderId, date);
                return ChannelInfoHelper.CreateFolder(id, date, null);
            });
        }

        private string GetChildFolderId(string folderId, string date)
        {
            switch (folderId)
            {
                case FolderIdOnDemand:
                    return OnDemandFolderProvider.CreateId(date);
                case FolderIdCondensed:
                    return CondensedFolderProvider.CreateId(date);
                case FolderIdHighlights:
                    return HighlightsFolderProvider.CreateId(date);
                default:
                    _logger.Error("Inside DatesFolderProvider, unknown folderId \"{0}\"", folderId);
                    throw new NotSupportedException(Resources.ThisIsABug);
            }
        }

        public static string CreateOnDemandId()
        {
            return FolderIdOnDemand;
        }

        public static string CreateCondensedId()
        {
            return FolderIdCondensed;
        }

        public static string CreateHighlightsId()
        {
            return FolderIdHighlights;
        }
    }
}
