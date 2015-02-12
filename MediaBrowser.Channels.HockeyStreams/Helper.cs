using MediaBrowser.Channels.HockeyStreams.Configuration;

namespace MediaBrowser.Channels.HockeyStreams
{
    internal static class Helper
    {
        internal static readonly string ChannelName = "Hockey Streams";
        internal static readonly string ChannelDescription = "Watch live and on-demand Hockey Streams from the NHL, AHL, OHL, QMJHL, WHL and more!";
        internal static readonly string HomePageUrl = "www.hockeystreams.com";

        internal static readonly string ApiKey = "d42ccf344acfcfb85b139c10eaa4d339";
        internal static readonly string ApiUrl = "https://api.hockeystreams.com/";

        // Increment as needed to invalidate all caches
        internal static readonly string DataVersion = "30";

        internal static string ConfigPageEmbededResourceUrl()
        {
            var type = typeof(ConfigurationPage);
            return type.Namespace + ".configPage.html";
        }
    }
}
