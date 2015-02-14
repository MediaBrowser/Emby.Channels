using System;

namespace MediaBrowser.Channels.LeagueOfLegends.Twitch
{
    internal class TwitchVideoId
    {
        private const string IdString = "twitchId";
        private const string TimeStartString = "timeStart";

        public string Id { get; private set; }
        public string TimeStart { get; private set; }

        public TwitchVideoId(string id, string timeStart)
        {
            Id = id;
            TimeStart = timeStart;
        }

        public TwitchVideoId(string twitchVideoId)
        {
            var match = Helpers.RegexMatch(twitchVideoId, "{0}-(?<id>.*)-{1}-(?<timeStart>.*)", IdString, TimeStartString);
            if (!match.Success)
            {
                throw new ArgumentException("Invalid id format: " + twitchVideoId, "twitchVideoId");
            }
            Id = match.Groups["id"].Value;
            TimeStart = match.Groups["timeStart"].Value;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Id))
            {
                return Helpers.PlaceholderId;
            }
            return string.Format("{0}-{1}-{2}-{3}", IdString, Id, TimeStartString, TimeStart);
        }
    }
}
