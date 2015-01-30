using System;
using System.Diagnostics;
using System.Reflection;
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

        internal static string UserAgent
        {
            get
            {
                var version = GetAssemblyVersion();
                return string.Format("Media Browser 3/{0} +http://mediabrowser.tv/", version);
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

        private static string GetAssemblyVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            // MediaBrowser.Channels.LeagueOfLegends, Version=1.0.5503.13083, Culture=neutral, PublicKeyToken=null
            var versionMatch = Regex.Match(assembly.FullName, ".*, Version=(.*?), .*");
            return versionMatch.Groups[1].Value;
        }
    }
}
