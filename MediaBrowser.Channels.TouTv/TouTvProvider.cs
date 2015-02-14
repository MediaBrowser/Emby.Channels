using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.TouTv.TouTvApi;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Channels;

namespace MediaBrowser.Channels.TouTv
{
    internal class TouTvProvider
    {
        private readonly PresentationService _presentationService;
        private readonly MediaValidationV1Service _mediaValidationV1Service;

        public TouTvProvider(PresentationService presentationService, MediaValidationV1Service mediaValidationV1Service)
        {
            _presentationService = presentationService;
            _mediaValidationV1Service = mediaValidationV1Service;
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetTypesOfMedia(CancellationToken cancellationToken)
        {
            var sections = await _presentationService.GetAlphabeticalSections(cancellationToken);
            return sections.Lineups.Select(CreateFolderChannelItemInfo);
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetShows(string title, CancellationToken cancellationToken)
        {
            var sections = await _presentationService.GetAlphabeticalSections(cancellationToken);
            var lineup = sections.Lineups.First(l => l.Title == title);
            return lineup.LineupItems
                .Where(IsValidItem)
                .Select(CreateFolderChannelItemInfo);
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetShowEpisodes(string url, CancellationToken cancellationToken)
        {
            var section = await _presentationService.GetSection(url, cancellationToken);
            if (section.SeasonLineups == null)
            {
                return await CreateChannelItemInfo(section.MediaUrl, cancellationToken);
            }
            return CreateChannelItemInfos(section.SeasonLineups);
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetEpisode(string url, CancellationToken cancellationToken)
        {
            var section = await _presentationService.GetSection(url, cancellationToken);
            var videoUrl = await _mediaValidationV1Service.GetVideoUrl(section.IdMedia, cancellationToken);
            return new List<ChannelMediaInfo>
            {
                new ChannelMediaInfo { Path = videoUrl.Url }
            };
        }

        private static bool IsValidItem(LineupItem item)
        {
            return item.IsActive && item.IsFree && (item.Template.Contains("-content") || item.Template == "program");
        }

        private ChannelItemInfo CreateFolderChannelItemInfo(Lineup lineup)
        {
            var folderId = FolderId.CreateSectionFolderId(lineup.Title);
            return new ChannelItemInfo
            {
                FolderType = ChannelFolderType.Container,
                Id = folderId.ToString(),
                Name = lineup.Title,
                Type = ChannelItemType.Folder
            };
        }

        private static ChannelItemInfo CreateFolderChannelItemInfo(LineupItem show)
        {
            var folderId = FolderId.CreateShowFolderId(show.Url);
            return new ChannelItemInfo
            {
                FolderType = ChannelFolderType.Container,
                Id = folderId.ToString(),
                ImageUrl = show.ImageUrl,
                Name = show.Title,
                Type = ChannelItemType.Folder
            };
        }

        private IEnumerable<ChannelItemInfo> CreateChannelItemInfos(IEnumerable<Lineup> seasonLineups)
        {
            return seasonLineups
                .Select(CreateChannelItemInfo)
                .SelectMany(items => items);
        }

        private IEnumerable<ChannelItemInfo> CreateChannelItemInfo(Lineup lineup)
        {
            return lineup.LineupItems
                .Where(item => item.IsActive)
                .Select(item => CreateChannelItemInfo(item, lineup.Title));
        }

        private ChannelItemInfo CreateChannelItemInfo(LineupItem episode, string season)
        {
            return new ChannelItemInfo
            {
                Id = episode.Url,
                ImageUrl = episode.ImageUrl,
                MediaType = ChannelMediaType.Video,
                Name = string.Format("{0}: {1}", season, episode.Title),
                Overview = episode.Details.Description,
                ProductionYear = GetProductionYear(episode.Details.ProductionYear),
                People = ConvertPeople(episode.Details.Persons),
                Type = ChannelItemType.Media
            };
        }

        private async Task<IEnumerable<ChannelItemInfo>> CreateChannelItemInfo(string url, CancellationToken cancellationToken)
        {
            var section = await _presentationService.GetSection(url, cancellationToken);
            if (section.SeasonLineups == null)
            {
                return new List<ChannelItemInfo>
                {
                    CreateChannelItemInfo(section.MediaUrl)
                };
            }
            return CreateChannelItemInfos(section.SeasonLineups);
        }

        private ChannelItemInfo CreateChannelItemInfo(string url)
        {
            return new ChannelItemInfo
            {
                Id = url,
                MediaType = ChannelMediaType.Video,
                Name = "Vidéo",
                Type = ChannelItemType.Media
            };
        }

        private List<PersonInfo> ConvertPeople(IEnumerable<KeyValuePair<string, string>> people)
        {
            if (people != null)
            {
                return people.Select(CreatePersonInfo).ToList();
            }
            return new List<PersonInfo>();
        }

        private static int? GetProductionYear(int? productionYear)
        {
            if (productionYear.HasValue && productionYear.Value != 0)
            {
                return productionYear;
            }

            return null;
        }

        private PersonInfo CreatePersonInfo(KeyValuePair<string, string> arg)
        {
            return new PersonInfo
            {
                Role = arg.Key,
                Name = arg.Value
            };
        }
    }
}
