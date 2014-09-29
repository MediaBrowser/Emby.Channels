using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.Trailers.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool EnableMovieArchive { get; set; }
        public bool ForceDownloadListings { get; set; }

        public PluginConfiguration()
        {
        }
    }
}
