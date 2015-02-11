using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;

namespace MediaBrowser.Channels.HockeyStreams.Media
{
    internal static class ChannelInfoHelper
    {
        private static string FavoriteTeam
        {
            get { return Plugin.Instance.Configuration.FavoriteTeam; }
        }

        public static ChannelItemInfo CreateFolder(string id, string name, string imageUrl)
        {
            return new ChannelItemInfo
            {
                FolderType = ChannelFolderType.Container,
                Genres = new List<string> { "Sports" },
                Id = id,
                ImageUrl = imageUrl,
                Name = name,
                Type = ChannelItemType.Folder
            };
        }

        public static ChannelItemInfo CreateChannelItemInfo(string id, string name, string overview, DateTime eventDate)
        {
            return new ChannelItemInfo
            {
                Genres = new List<string> { "Sports" },
                Id = id,
                MediaType = ChannelMediaType.Video,
                Name = name,
                Overview = overview,
                PremiereDate = eventDate,
                ProductionYear = eventDate.Year
            };
        }

        public static DateTime ParseDate(string date)
        {
            return DateTime.ParseExact(date, "MM/dd/yyyy", CultureInfo.InvariantCulture);
        }

        public static string FirstNotNull(params string[] strings)
        {
            return strings.FirstOrDefault(str => !string.IsNullOrEmpty(str));
        }

        public static string FormatMatchName(string homeTeam, string awayTeam)
        {
            return string.Format("{0} @ {1}", awayTeam, homeTeam);
        }

        public static string FormatFavoriteMatchName(string homeTeam, string awayTeam)
        {
            if (FavoriteTeam == homeTeam)
            {
                return "vs " + awayTeam;
            }
            return "vs " + homeTeam;
        }
    }
}
