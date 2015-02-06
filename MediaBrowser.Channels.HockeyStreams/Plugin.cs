using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Channels.HockeyStreams.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.HockeyStreams
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        internal const string ChannelName = "Hockey Streams";
        internal const string ChannelDescription = "Watch live and on-demand Hockey Streams from the NHL, AHL, OHL, QMJHL, WHL and more!";

        public override string Name
        {
            get { return ChannelName; }
        }

        public override string Description
        {
            get { return ChannelDescription; }
        }

        internal static Plugin Instance { get; private set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }
    }
}
