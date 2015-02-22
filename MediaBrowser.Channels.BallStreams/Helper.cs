namespace MediaBrowser.Channels.HockeyStreams
{
    internal static class Helper
    {
        internal const string ChannelName = "Ball Streams";
        internal const string ChannelDescription = "Watch live and on-demand Ball Streams from the NBA!";
        internal const string HomePageUrl = "www.ballstreams.com";

        internal const string ApiKey = "dadacfdf972ac6da3dd2171fd46f3ef5";
        internal const string ApiUrl = "https://api.ballstreams.com/";

        // Increment as needed to invalidate all caches
        internal const string DataVersion = "1";

        internal static string ConfigPageEmbededResourceUrl()
        {
            return "MediaBrowser.Channels.BallStreams.Configuration.configPage.html";
        }
    }
}
