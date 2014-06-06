using MediaBrowser.Model.Plugins;
using System;

namespace PodCasts.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {

        /// <summary>
        /// List of feeds
        /// </summary>
        /// <value>urls of xml podcast feeds</value>
        public string[] Feeds { get; set; }

        /// <summary>
        /// Tracks the last time we successfully updates the feeds
        /// </summary>
        public DateTime LastFeedUpdate { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public PluginConfiguration()
        {
            Feeds = new string[] {};

        }
    }
}
