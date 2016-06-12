using MediaBrowser.Controller.Plugins;

namespace MediaBrowser.Plugins.SoundCloud
{
    public class EntryPoint : IServerEntryPoint
    {
        public void Run()
        {
            Plugin.Instance.AttemptLogin(true);
        }

        public void Dispose()
        {
        }
    }
}
