using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Channels.HockeyStreams.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string Username { get; set; }
        public string Token { get; set; }
        public string FavoriteTeam { get; set; }
    }
}
