using System;
using System.IO;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;

namespace MediaBrowser.Channels.HockeyStreams.Configuration
{
    internal class ConfigurationPage : IPluginConfigurationPage
    {
        public string Name
        {
            get { return Helper.ChannelName.Replace(" ", ""); }
        }

        public ConfigurationPageType ConfigurationPageType
        {
            get { return ConfigurationPageType.PluginConfiguration; }
        }

        public IPlugin Plugin
        {
            get { return HockeyStreams.Plugin.Instance; }
        }

        public Stream GetHtmlStream()
        {
            var configPage = Helper.ConfigPageEmbededResourceUrl();
            return GetType().Assembly.GetManifestResourceStream(configPage);
        }
    }
}
