using MediaBrowser.Model.Plugins;

namespace ChannelDownloader.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string DownloadPath { get; set; }
        public int? MaxDownloadAge { get; set; }
        public string[] DownloadingChannels { get; set; }
        public double? DownloadSizeLimit { get; set; }

        public PluginConfiguration()
        {
            DownloadingChannels = new string[] { };
            DownloadSizeLimit = .5;
            MaxDownloadAge = 30;
        }
    }
}
