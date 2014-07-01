using MediaBrowser.Model.Plugins;
using System;

namespace MediaBrowser.Plugins.SoundCloud.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public String Username { get; set; }
        public String Password { get; set; }
        public String PwData { get; set; }
    }
}
