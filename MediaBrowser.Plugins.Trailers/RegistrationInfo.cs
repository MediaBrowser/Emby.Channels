using MediaBrowser.Common.Security;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers
{
    public class RegistrationInfo : IRequiresRegistration
    {
        private readonly ISecurityManager _securityManager;

        public static RegistrationInfo Instance;

        public RegistrationInfo(ISecurityManager securityManager)
        {
            _securityManager = securityManager;
            Instance = this;
        }

        private bool _registered;
        public bool IsRegistered
        {
            get { return _registered; }
        }

        public async Task LoadRegistrationInfoAsync()
        {
            var info = await _securityManager.GetRegistrationStatus("Trailers").ConfigureAwait(false);

            _registered = info.IsValid;
        }
    }
}
