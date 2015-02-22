using MediaBrowser.Channels.HockeyStreams.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.HockeyStreams
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public override string Name
        {
            get { return Helper.ChannelName; }
        }

        public override string Description
        {
            get { return Helper.ChannelDescription; }
        }

        internal static Plugin Instance { get; private set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }
    }
}
