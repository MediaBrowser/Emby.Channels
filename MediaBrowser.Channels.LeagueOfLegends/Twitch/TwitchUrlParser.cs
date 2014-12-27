using System.Text.RegularExpressions;

namespace MediaBrowser.Channels.LeagueOfLegends.Twitch
{
    internal class TwitchUrlParser
    {
        public string Id { get; private set; }
        public string TimeStart { get; private set; }

        public TwitchUrlParser(string url)
        {
            var match = Helpers.RegexMatch(url, @"https?://www.twitch.tv/.*?/.*?/(?<id>\d*)(\?t=(?<timeStart>.*))?");
            Id = match.Groups["id"].Value;
            SetTimeStart(match);
        }

        private void SetTimeStart(Match match)
        {
            var timeStart = match.Groups["timeStart"];
            if (timeStart != null)
            {
                TimeStart = timeStart.Value;
            }
        }
    }
}
