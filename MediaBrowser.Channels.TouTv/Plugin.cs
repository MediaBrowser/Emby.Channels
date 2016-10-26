using System.Collections.Generic;
using MediaBrowser.Channels.TouTv.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.TouTv
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        internal static string ChannelName = "ICI Tou.tv";

        internal static string ChannelDescription =
            "ICI Tou.tv est une webtélé de divertissement proposant une expérience de vidéo sur demande offerte par Radio-Canada.";

        internal static Plugin Instance { get; private set; }

        public override string Name
        {
            get { return ChannelName; }
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "toutv",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
                }
            };
        }

        public override string Description
        {
            get { return ChannelDescription; }
        }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }
    }
}
