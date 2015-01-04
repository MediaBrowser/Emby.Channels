using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.SoundCloud.Configuration;
using SoundCloud.NET;

namespace MediaBrowser.Plugins.SoundCloud
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public SoundCloudClient SoundCloudClient;
        private readonly IEncryptionManager _encryption;
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IEncryptionManager encryption)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _encryption = encryption;

            var username = Instance.Configuration.Username;
            var password = Instance.Configuration.PwData;

            var creds = new SoundCloudCredentials("78fd88dde7ebf8fdcad08106f6d56ab6",
                    "ef6b3dbe724eff1d03298c2e787a69bd");

            if (username != null && password != null)
            {
                creds = new SoundCloudCredentials("78fd88dde7ebf8fdcad08106f6d56ab6",
                    "ef6b3dbe724eff1d03298c2e787a69bd", username, _encryption.DecryptString(Instance.Configuration.PwData));
            }

            SoundCloudClient = new SoundCloudClient(creds);
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "SoundCloud"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "SoundCloud music in your collection.";
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }

        public override void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            var config = (PluginConfiguration) configuration;

            // Encrypt password for saving.  The Password field the config page sees will always be blank except when updated.
            // The program actually uses the encrypted version

            config.PwData = _encryption.EncryptString(config.Password ?? string.Empty);
            config.Password = null;
           

            base.UpdateConfiguration(configuration);
        }
    }
}
