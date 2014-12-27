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
                Assembly assembly = Assembly.GetExecutingAssembly();
                var assemblyUri = new Uri(assembly.CodeBase);
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assemblyUri.LocalPath);
                string version = fileVersionInfo.FileVersion;
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
    }
}
