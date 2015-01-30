using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.LeagueOfLegends.LolEventVods;
using MediaBrowser.Channels.LeagueOfLegends.Twitch;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using Match = MediaBrowser.Channels.LeagueOfLegends.LolEventVods.Match;

namespace MediaBrowser.Channels.LeagueOfLegends
{
    internal class LolVideoProvider
    {
        private readonly ILogger _logger;
        private readonly LolEventVodsService _lolEventVodsService;

        public LolVideoProvider(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost, ILogger logger)
        {
            _logger = logger;
            _lolEventVodsService = new LolEventVodsService(httpClient, jsonSerializer, applicationHost);
        }

        public async Task<ChannelItemResult> FindEvents(int limit, int offset, CancellationToken cancellationToken)
        {
            var events = await GetEvents(limit, offset, cancellationToken);
            var result = new ChannelItemResult
            {
                Items = events.Items
                    .Where(evt => evt.Status != EventStatus.None)
                    .Select(CreateChannelItemFolders)
                    .ToList()
            };
            result.TotalRecordCount = SetTotalRecordCount(limit, offset, result.Items.Count, events.After);
            return result;
        }

        public async Task<ChannelItemResult> FindDays(string eventId, CancellationToken cancellationToken)
        {
            var days = await _lolEventVodsService.GetEventDays(eventId, cancellationToken);
            return new ChannelItemResult
            {
                Items = days
                    .Where(day => !string.IsNullOrEmpty(day.Title))
                    .Select(day => CreateChannelItemFolders(eventId, day))
                    .ToList(),
                TotalRecordCount = days.Count()
            };
        }

        public async Task<ChannelItemResult> FindMatches(string eventId, string dayId, CancellationToken cancellationToken)
        {
            var days = await _lolEventVodsService.GetEventDays(eventId, cancellationToken);
            var day = days.FirstOrDefault(d => d.DayId == dayId);
            if (day == null)
            {
                _logger.Warn("DayId \"{0}\" couldn't be found in EventId \"{1}\"", dayId, eventId);
                return new ChannelItemResult();
            }

            var result = new ChannelItemResult { Items = new List<ChannelItemInfo>() };
            if (day.FullStream != null)
            {
                result.Items.Add(CreateChannelItemVideo(day));
            }
            result.Items.AddRange(day.Matches
                .Select(match => CreateChannelItemFolders(eventId, dayId, match)));
            result.TotalRecordCount = result.Items.Count;
            return result;
        }

        public async Task<ChannelItemResult> FindGames(string eventId, string dayId, string gameId, CancellationToken cancellationToken)
        {
            var days = await _lolEventVodsService.GetEventDays(eventId, cancellationToken);
            var day = days.FirstOrDefault(d => d.DayId == dayId);
            if (day == null)
            {
                _logger.Warn("DayId \"{0}\" couldn't be found in EventId \"{1}\"", dayId, eventId);
                return new ChannelItemResult();
            }
            var match = day.Matches.FirstOrDefault(m => m.GameId == gameId);
            if (match == null)
            {
                _logger.Warn("GameId \"{0}\" couldn't be found in DayId \"{0}\" and EventId \"{1}\"", gameId, dayId, eventId);
                return new ChannelItemResult();
            }

            return new ChannelItemResult
            {
                Items = match.VideoLinks
                    .Select(CreateChannelItemVideo)
                    .ToList(),
                TotalRecordCount = match.VideoLinks.Count()
            };
        }

        private async Task<Events> GetEvents(int limit, int offset, CancellationToken cancellationToken)
        {
            var pageNumber = offset / limit;
            var events = new Events { After = null };
            for (int i = 0; i <= pageNumber; i++)
            {
                events = await _lolEventVodsService.GetEvents(10, events.After, cancellationToken);
            }
            return events;
        }

        private static int SetTotalRecordCount(int limit, int offset, int count, string after)
        {
            if (string.IsNullOrEmpty(after))
            {
                return count;
            }
            return limit + offset + 1;
        }

        private ChannelItemInfo CreateChannelItemFolders(Event evt)
        {
            var folderId = FolderId.CreateEventFolderId(evt.EventId);
            return new ChannelItemInfo
            {
                DateCreated = evt.CreatedOn,
                FolderType = ChannelFolderType.Container,
                Id = folderId.ToString(),
                ImageUrl = evt.ImageUrl,
                Name = evt.Title,
                Type = ChannelItemType.Folder
            };
        }

        private ChannelItemInfo CreateChannelItemFolders(string eventId, Day day)
        {
            var folderId = FolderId.CreateDayFolderId(eventId, day.DayId);
            return new ChannelItemInfo
            {
                FolderType = ChannelFolderType.Container,
                Id = folderId.ToString(),
                ImageUrl = day.ImageUrl,
                Name = day.Title,
                Type = ChannelItemType.Folder
            };
        }

        private ChannelItemInfo CreateChannelItemFolders(string eventId, string dayId, Match match)
        {
            var folderId = FolderId.CreateGameFolderId(eventId, dayId, match.GameId);
            return new ChannelItemInfo
            {
                FolderType = ChannelFolderType.Container,
                Id = folderId.ToString(),
                Name = string.Format("{0} vs {1}", match.Team1, match.Team2),
                Type = ChannelItemType.Folder
            };
        }

        private ChannelItemInfo CreateChannelItemVideo(Day day)
        {
            var twitchVideoId = new TwitchVideoId(day.FullStream.TwitchId, day.FullStream.TimeStart);
            return new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.Clip,
                Id = twitchVideoId.ToString(),
                ImageUrl = day.ImageUrl,
                MediaType = ChannelMediaType.Video,
                Name = day.FullStream.Title,
                Type = ChannelItemType.Media
            };
        }

        private ChannelItemInfo CreateChannelItemVideo(VideoLink videoLink)
        {
            var twitchVideoId = new TwitchVideoId(videoLink.TwitchId, videoLink.TimeStart);
            return new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.Clip,
                Id = twitchVideoId.ToString(),
                MediaType = ChannelMediaType.Video,
                Name = videoLink.Title,
                Type = ChannelItemType.Media
            };
        }
    }
}
