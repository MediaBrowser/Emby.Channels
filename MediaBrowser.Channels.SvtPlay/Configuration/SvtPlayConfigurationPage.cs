using MediaBrowser.Controller.Plugins;
using System;

namespace MediaBrowser.Channels.SvtPlay.Configuration
{
    class SvtPlayConfigurationPage : IPluginConfigurationPage
    {
        public ConfigurationPageType ConfigurationPageType
        {
            get { return ConfigurationPageType.PluginConfiguration; }
        }

        public System.IO.Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream("MediaBrowser.Channels.SvtPlay.Configuration.configPage.html");
        }

        public string Name
        {
            get { return "Svt Play"; }
        }

        public Common.Plugins.IPlugin Plugin
        {
            get { return SvtPlay.Plugin.Instance; }
        }
    }
}
