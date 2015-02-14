using System.IO;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;

namespace MediaBrowser.Channels.HockeyStreams.Configuration
{
    internal class ConfigurationPage : IPluginConfigurationPage
    {
        public string Name
        {
            get { return HockeyStreams.Plugin.ChannelName.Replace(" ", ""); }
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
            var type = GetType();
            var configPage = type.Namespace + ".configPage.html";
            return type.Assembly.GetManifestResourceStream(configPage);
        }
    }
}
