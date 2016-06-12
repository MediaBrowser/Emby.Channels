using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.SoundCloud.ClientApi;
using MediaBrowser.Plugins.SoundCloud.Configuration;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace MediaBrowser.Plugins.SoundCloud
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        private readonly IEncryptionManager _encryption;
        private readonly ILogger _logger;
        private readonly INotificationManager _notificationManager;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        private SoundCloudClient soundCloudClient;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IEncryptionManager encryption, ILogManager logManager, INotificationManager notificationManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _encryption = encryption;
            _logger = logManager.GetLogger(GetType().Name);
            _notificationManager = notificationManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;

            soundCloudClient = new SoundCloudClient(_logger, _jsonSerializer, _httpClient);
        }

        public void AttemptLogin(bool createNotificationOnFailure)
        {
            var username = Instance.Configuration.Username;
            var password = _encryption.DecryptString(Instance.Configuration.PwData);

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                try
                {
                    soundCloudClient.Authenticate(username, password);
                }
                catch (Exception ex)
                {
                    var msg = "Unable to login to SoundCloud. Please check username and password.";

                    //if (!string.IsNullOrWhiteSpace(ex.ResponseBody))
                    //{
                    //    msg = string.Format("{0} ({1})", msg, ex.ResponseBody);
                    //}

                    _logger.ErrorException(msg, ex);

                    if (createNotificationOnFailure)
                    {
                        var request = new NotificationRequest
                        {
                            Description = msg,
                            Date = DateTime.Now,
                            Level = NotificationLevel.Error,
                            SendToUserMode = SendToUserType.Admins
                        };

                        _notificationManager.SendNotification(request, CancellationToken.None);
                    }
                    else
                    {
                        msg = string.Format("{0}\n\nAttention: You need to wait up to 3 minutes before retrying!", msg);
                        throw new Exception(msg);
                    }
                }
            }
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

        public bool IsAuthenticated
        {
            get
            {
                return soundCloudClient.IsAuthenticated;
            }
        }

        public SoundCloudClient Client
        {
            get
            {
                return soundCloudClient;
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }

        public override void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            var config = (PluginConfiguration)configuration;

            // Encrypt password for saving.  The Password field the config page sees will always be blank except when updated.
            // The program actually uses the encrypted version

            config.PwData = _encryption.EncryptString(config.Password ?? string.Empty);
            config.Password = null;

            base.UpdateConfiguration(configuration);

            // This will throw with invalid credentials
            this.AttemptLogin(false);
        }
    }
}
