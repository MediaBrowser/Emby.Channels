using MediaBrowser.Controller.Plugins;

namespace MediaBrowser.Plugins.Vimeo
{
    public class EntryPoint : IServerEntryPoint
    {
        public void Dispose()
        {
        }

        public void Run()
        {
            Plugin.Instance.Login();
        }
    }
}
