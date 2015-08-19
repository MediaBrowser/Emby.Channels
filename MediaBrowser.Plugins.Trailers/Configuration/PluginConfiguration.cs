using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.Trailers.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool EnableMovieArchive { get; set; }
        public bool EnableNetflix { get; set; }
        public bool EnableDvd { get; set; }
        public bool EnableTheaters { get; set; }

        public bool ExcludeUnIdentifiedContent { get; set; }
        
        public PluginConfiguration()
        {
            EnableNetflix = true;
            EnableDvd = true;
            EnableTheaters = true;
            ExcludeUnIdentifiedContent = true;
        }
    }
}
