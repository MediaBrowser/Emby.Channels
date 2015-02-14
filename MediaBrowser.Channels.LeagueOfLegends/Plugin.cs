using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.LeagueOfLegends
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        internal static string ChannelName = "League of Legends";
        internal static string ChannelDescription = "Spoiler-free VoDs of League of Legends action in the LCS.";

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        { }

        public override string Name
        {
            get { return ChannelName; }
        }

        public override string Description
        {
            get { return ChannelDescription; }
        }
    }
}
