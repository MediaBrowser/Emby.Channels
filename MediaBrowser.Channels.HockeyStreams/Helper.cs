using MediaBrowser.Channels.HockeyStreams.Configuration;

namespace MediaBrowser.Channels.HockeyStreams
{
    internal static class Helper
    {
        internal const string ChannelName = "Hockey Streams";
        internal const string ChannelDescription = "Watch live and on-demand Hockey Streams from the NHL, AHL, OHL, QMJHL, WHL and more!";
        internal const string HomePageUrl = "www.hockeystreams.com";

        internal const string ApiKey = "d42ccf344acfcfb85b139c10eaa4d339";
        internal const string ApiUrl = "https://api.hockeystreams.com/";

        // Increment as needed to invalidate all caches
        internal const string DataVersion = "30";
        internal const string LoginRoute = "/BallStreams/Auth/Login";

        internal static string ConfigPageEmbededResourceUrl()
        {
            var type = typeof(ConfigurationPage);
            return type.Namespace + ".configPage.html";
        }
    }
}
