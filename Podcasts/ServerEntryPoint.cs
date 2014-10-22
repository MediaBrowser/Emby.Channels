using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Entities;
using PodCasts.Configuration;
using MediaBrowser.Model.Logging;

namespace PodCasts
{
    /// <summary>
    /// Class ServerEntryPoint
    /// </summary>
    public class ServerEntryPoint : IServerEntryPoint, IRequiresRegistration
    {
        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static ServerEntryPoint Instance { get; private set; }

        /// <summary>
        /// The _task manager
        /// </summary>
        private readonly ITaskManager _taskManager;

        public ILibraryManager LibraryManager { get; private set; }
        public IConfigurationManager ConfigurationManager { get; set; }
        public IApplicationPaths ApplicationPaths { get; set; }
        public ISecurityManager PluginSecurityManager { get; set; }
        public IItemRepository ItemRepository { get; set; }
        public IUserManager UserManager { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerEntryPoint" /> class.
        /// </summary>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="securityManager">The security manager.</param>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="repo">The repo.</param>
        /// <param name="userManager">The user manager.</param>
        public ServerEntryPoint(ITaskManager taskManager, ILibraryManager libraryManager, ILogManager logManager, ISecurityManager securityManager,
            IApplicationPaths applicationPaths, IServerConfigurationManager configurationManager, IItemRepository repo, IUserManager userManager)
        {
            _taskManager = taskManager;
            LibraryManager = libraryManager;
            ConfigurationManager = configurationManager;
            ApplicationPaths = applicationPaths;
            ItemRepository = repo;
            
            UserManager = userManager;
            PluginSecurityManager = securityManager;
            Plugin.Logger = logManager.GetLogger("Podcasts");

            Instance = this;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
           
        }

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        /// <param name="oldConfig">The old config.</param>
        /// <param name="newConfig">The new config.</param>
        public void OnConfigurationUpdated(PluginConfiguration oldConfig, PluginConfiguration newConfig)
        {
            
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Loads our registration information
        ///
        /// </summary>
        /// <returns></returns>
        public async Task LoadRegistrationInfoAsync()
        {
            Plugin.Instance.Registration = await PluginSecurityManager.GetRegistrationStatus("PodCasts").ConfigureAwait(false);
            Plugin.Logger.Debug("PodCasts Registration Status - Registered: {0} In trial: {2} Expiration Date: {1} Is Valid: {3}", Plugin.Instance.Registration.IsRegistered, Plugin.Instance.Registration.ExpirationDate, Plugin.Instance.Registration.TrialVersion, Plugin.Instance.Registration.IsValid);
        }
    }
}
