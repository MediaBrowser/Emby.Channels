using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.SoundCloud.ClientApi;
using MediaBrowser.Plugins.SoundCloud.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace MediaBrowser.Plugins.SoundCloud
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        private readonly IEncryptionManager _encryption;
        private readonly ILogger _logger;
        private readonly INotificationManager _notificationManager;
        private readonly IChannelManager _channelManager;

        private readonly SoundCloudClient _soundCloudClient;

        private List<string> _resourceNames = new List<string>();
        private readonly object _saveLock = new object();
        private string _ownChannelId;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IEncryptionManager encryption, ILogManager logManager, INotificationManager notificationManager, IJsonSerializer jsonSerializer, IHttpClient httpClient, IChannelManager channelManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _encryption = encryption;
            _logger = logManager.GetLogger(GetType().Name);
            _notificationManager = notificationManager;
            _channelManager = channelManager;

            _soundCloudClient = new SoundCloudClient(_logger, jsonSerializer, httpClient);
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "soundcloud",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
                }
            };
        }

        public void AttemptLogin(bool createNotificationOnFailure)
        {
            var username = Instance.Configuration.Username;
            var password = _encryption.DecryptString(Instance.Configuration.PwData);

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                try
                {
                    _soundCloudClient.Authenticate(username, password);
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
                return _soundCloudClient.IsAuthenticated;
            }
        }

        public SoundCloudClient Client
        {
            get
            {
                return _soundCloudClient;
            }
        }

        internal ILogger Logger
        {
            get { return this._logger; }
        }

        internal IChannelManager ChannelManager
        {
            get { return _channelManager; }
        }

        internal string OwnChannelId
        {
            get
            {
                if (_ownChannelId == null)
                {
                    _ownChannelId = string.Empty;
                    foreach (var channel in _channelManager.GetAllChannelFeatures())
                    {
                        if (channel.Name == SoundCloudChannel.ChannelName)
                        {
                            _ownChannelId = channel.Id;
                        }
                    }
                }

                return _ownChannelId;
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

        public string GetResourceCachePath(string category, string name)
        {
            if (!Path.HasExtension(name))
            {
                name = string.Concat(name, ".png");
            }
            return Path.Combine(new string[] { base.ApplicationPaths.CachePath, "soundcloud", base.Version.ToString(), category, name });
        }

        public string GetTempDirectory()
        {
            return base.ApplicationPaths.TempDirectory;
        }

        public string GetExtractedResourceFilePath(string name)
        {
            string str;
            string manifestStreamName = this.GetManifestStreamName(name);
            if (!string.IsNullOrWhiteSpace(manifestStreamName))
            {
                string extension = Path.GetExtension(manifestStreamName);
                if (!string.IsNullOrWhiteSpace(extension))
                {
                    name = string.Concat(name, extension);
                }
                else
                {
                    _logger.Error(string.Concat("Resource found without extension: ", manifestStreamName), new object[0]);
                }
                string resourceCachePath = this.GetResourceCachePath("resources", name);
                if (!File.Exists(resourceCachePath))
                {
                    lock (this._saveLock)
                    {
                        if (!File.Exists(resourceCachePath))
                        {
                            string str1 = Path.Combine(base.ApplicationPaths.TempDirectory, string.Concat(Guid.NewGuid(), Path.GetExtension(resourceCachePath)));
                            Directory.CreateDirectory(Path.GetDirectoryName(str1));
                            Directory.CreateDirectory(Path.GetDirectoryName(resourceCachePath));
                            using (Stream manifestResourceStream = base.GetType().Assembly.GetManifestResourceStream(manifestStreamName))
                            {
                                using (FileStream fileStream = new FileStream(str1, FileMode.Create, FileAccess.Write, FileShare.Read))
                                {
                                    manifestResourceStream.CopyTo(fileStream);
                                }
                            }
                            try
                            {
                                File.Copy(str1, resourceCachePath, false);
                            }
                            catch (Exception exception)
                            {
                            }
                            str = str1;
                            return str;
                        }
                    }
                }
                str = resourceCachePath;
            }
            else
            {
                _logger.Error(string.Concat("Resource not found: ", name), new object[0]);
                str = null;
            }
            return str;
        }

        private string GetManifestStreamName(string name)
        {
            string str = name;
            str = string.Concat(base.GetType().Namespace, ".Images.", str);
            string str1 = this.GetManifestStreamNames().FirstOrDefault<string>((string i) => (string.Equals(i, string.Concat(str, ".png"), StringComparison.OrdinalIgnoreCase) ? true : string.Equals(i, string.Concat(str, ".jpg"), StringComparison.OrdinalIgnoreCase)));
            return str1;
        }

        private IEnumerable<string> GetManifestStreamNames()
        {
            lock (_resourceNames)
            {
                if (_resourceNames.Count == 0)
                {
                    _resourceNames = base.GetType().Assembly.GetManifestResourceNames().ToList<string>();
                }

                return _resourceNames;
            }
        }
    }

}
