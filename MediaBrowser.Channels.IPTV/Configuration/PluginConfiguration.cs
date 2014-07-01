using System.Collections.Generic;
using MediaBrowser.Model.Plugins;
using System;

namespace MediaBrowser.Channels.IPTV.Configuration
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
        public List<Streams> streams { get; set; }

        /// <summary>
        /// Tracks the last time we successfully updates the feeds
        /// </summary>
        public DateTime LastFeedUpdate { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public PluginConfiguration()
        {
            streams = new List<Streams>();

        }
    }

    public class Streams
    {
        public String Name { get; set; }
        public String Image { get; set; }
        public String URL { get; set; }
        public String Type { get; set; }
    }
}
