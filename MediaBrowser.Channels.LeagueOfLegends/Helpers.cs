using System;
using System.Text.RegularExpressions;

namespace MediaBrowser.Channels.LeagueOfLegends
{
    internal static class Helpers
    {
        private const string PlaceholderString = "Spoiler-free placeholder - ";

        internal static string PlaceholderId
        {
            get
            {
                return PlaceholderString + Guid.NewGuid();
            }
        }

        internal static Match RegexMatch(string input, string patternFormat, params object[] args)
        {
            string pattern = string.Format(patternFormat, args);
            return Regex.Match(input, pattern);
        }

        internal static bool IsPlaceholderId(string id)
        {
            return !string.IsNullOrEmpty(id) && id.StartsWith(PlaceholderString);
        }
    }
}
