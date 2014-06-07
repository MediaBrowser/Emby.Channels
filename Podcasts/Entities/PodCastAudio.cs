using MediaBrowser.Controller.Entities.Audio;

namespace PodCasts.Entities
{
    class PodCastAudio : Audio, IHasRemoteImage
    {
        public string RemoteImagePath { get; set; }

        public bool HasChanged(IHasRemoteImage copy)
        {
            return copy.RemoteImagePath != this.RemoteImagePath;
        }
    }
}
