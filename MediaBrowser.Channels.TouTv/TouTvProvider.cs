using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.TouTv.TouTvApi;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;

namespace MediaBrowser.Channels.TouTv
{
    internal class TouTvProvider
    {
        private readonly ITouTVAPIService _service;

        public TouTvProvider()
        {
            var binding = new BasicHttpBinding
            {
                MaxReceivedMessageSize = 50 * 1024 * 1024 // 50 MB, let's hope it's enough
            };
            var endpointAddress = new EndpointAddress("http://api.tou.tv/v1/TouTVAPIService.svc");
            _service = new TouTVAPIServiceClient(binding, endpointAddress);
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetGenres()
        {
            var genres = await _service.GetGenresAsync();
            return genres.Select(CreateChannelItemInfo);
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetGenreShows(string genreId)
        {
            long genreIdLong = Convert.ToInt64(genreId, CultureInfo.InvariantCulture);
            var shows = await _service.GetEmissionsAsync();
            return shows
                .Where(show => show.Genre.Id == genreIdLong)
                .Where(ShowIsAvailable)
                .Select(CreateChannelItemInfo);
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetShows()
        {
            var shows = await _service.GetEmissionsAsync();
            return shows
                .Where(ShowIsAvailable)
                .Select(CreateChannelItemInfo);
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetShowEpisodes(string showId)
        {
            long emissionId = Convert.ToInt64(showId, CultureInfo.InvariantCulture);
            var episodes = await _service.GetEpisodesForEmissionAsync(emissionId);
            return episodes
                .Reverse()
                .Select(dto => CreateChannelItemInfo(dto, showId));
        }

        public async Task<EpisodeDTO> GetEpisode(string showId, string episodeId)
        {
            long emissionId = Convert.ToInt64(showId, CultureInfo.InvariantCulture);
            long epId = Convert.ToInt64(episodeId, CultureInfo.InvariantCulture);
            var episodes = await _service.GetEpisodesForEmissionAsync(emissionId);
            return episodes.First(ep => ep.Id == epId);
        }

        public async Task<IEnumerable<ChannelItemInfo>> SearchShow(string searchTerm, CancellationToken cancellationToken)
        {
            var searchResult = await _service.SearchTermsAsync(searchTerm);
            return searchResult.Results.Select(CreateChannelItemInfo);
        }

        private ChannelItemInfo CreateChannelItemInfo(GenreDTO genre)
        {
            var folderId = FolderId.CreateGenreFolderId(genre.Id);
            return new ChannelItemInfo
            {
                FolderType = ChannelFolderType.Container,
                Id = folderId.ToString(),
                ImageUrl = genre.ImageBackground,
                Name = genre.Title,
                Type = ChannelItemType.Folder
            };
        }

        private ChannelItemInfo CreateChannelItemInfo(EmissionDTO show)
        {
            var folderId = FolderId.CreateShowFolderId(show.Id);
            return new ChannelItemInfo
            {
                FolderType = ChannelFolderType.Container,
                Genres = new List<string> { show.Genre.Title },
                Id = folderId.ToString(),
                ImageUrl = GetShowImageUrl(show.CategoryURL),
                Name = show.Title,
                Overview = show.Description,
                ProductionYear = Convert.ToInt32(show.Year),
                Type = ChannelItemType.Folder,
            };
        }

        private ChannelItemInfo CreateChannelItemInfo(EpisodeDTO episode, string showId)
        {
            var episodeId = episode.Id.ToString(CultureInfo.InvariantCulture);
            var ep = new Episode(showId, episodeId);
            var name = FormatEpisodeName(episode);
            return new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.Episode,
                Id = ep.ToString(),
                ImageUrl = episode.ImagePlayerLargeA,
                MediaType = ChannelMediaType.Video,
                Name = name,
                PremiereDate = episode.OriginalAirDate,
                RunTimeTicks = episode.LengthSpan.Ticks,
                Type = ChannelItemType.Media
            };
        }

        private static string FormatEpisodeName(EpisodeDTO episode)
        {
            if (string.IsNullOrEmpty(episode.SeasonAndEpisode))
            {
                return episode.Title;
            }
            return string.Format("[{0}] {1}", episode.SeasonAndEpisode, episode.Title);
        }

        private ChannelItemInfo CreateChannelItemInfo(SearchResultDataDTO searchResultData)
        {
            if (searchResultData.ResultType == SearchResultDataTypeEnum.Emission)
            {
                return CreateChannelItemInfo(searchResultData.Emission);
            }
            else
            {
                return null;
                //return CreateChannelItemInfo(searchResultData.Episode, searchResultData.Episode.)
            }
        }

        private string GetShowImageUrl(string showName)
        {
            var trimmedShowName = showName
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .Replace("'", "");
            return string.Format("http://images.tou.tv/w_900,h_506,c_scale/v1/emissions/16x9/{0}.jpg", trimmedShowName);
        }

        private static bool ShowIsAvailable(EmissionDTO show)
        {
            return show.DateRetraitOuEmbargo == DateTime.MinValue || show.DateRetraitOuEmbargo >= DateTime.Now;
        }
    }
}
