using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.Trailers.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Trailers older than this will not be downloaded and deleted if already downloaded.
        /// </summary>
        /// <value>The max trailer age.</value>
        public int? MaxTrailerAge { get; set; }
    }
}
