
namespace PodCasts.Entities
{
    interface IHasRemoteImage
    {
        string RemoteImagePath { get; set; }
        string PrimaryImagePath { get; }
        bool HasChanged(IHasRemoteImage copy);
    }
}
