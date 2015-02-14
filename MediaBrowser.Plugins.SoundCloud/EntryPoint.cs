using MediaBrowser.Controller.Plugins;

namespace MediaBrowser.Plugins.SoundCloud
{
    public class EntryPoint : IServerEntryPoint
    {
        public void Run()
        {
            var username = Plugin.Instance.Configuration.Username;
            var password = Plugin.Instance.Configuration.PwData;

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                Plugin.Instance.SoundCloudClient.Authenticate();
            }
        }

        public void Dispose()
        {
        }
    }
}
