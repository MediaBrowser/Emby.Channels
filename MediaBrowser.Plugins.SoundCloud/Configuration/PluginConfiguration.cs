using MediaBrowser.Model.Plugins;
using MediaBrowser.Plugins.SoundCloud;
using System;
using System.Runtime.Serialization;

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

        public bool IsAuthenticated
        {
            get
            {
                return Plugin.Instance.IsAuthenticated;
            }
        }
    }
}
