using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.SoundCloud.ExternalIds
{
    public class SoundCloudUserId : IExternalId
    {
        public string Name
        {
            get { return "SoundCloud"; }
        }

        public string Key
        {
            get { return "scuser"; }
        }

        public string UrlFormatString
        {
            get { return null; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is MusicArtist;
        }
    }

    public class SoundCloudUserLink : IExternalId
    {
        public string Name
        {
            get { return "SoundCloud User Page"; }
        }

        public string Key
        {
            get { return "scuserlink"; }
        }

        public string UrlFormatString
        {
            get { return "{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is MusicArtist;
        }
    }

    public class SoundCloudTrackId : IExternalId
    {
        public string Name
        {
            get { return "SoundCloud"; }
        }

        public string Key
        {
            get { return "sctrack"; }
        }

        public string UrlFormatString
        {
            get { return null; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio;
        }
    }

    public class SoundCloudTrackLink : IExternalId
    {
        public string Name
        {
            get { return "Track on SoundCloud"; }
        }

        public string Key
        {
            get { return "sctracklink"; }
        }

        public string UrlFormatString
        {
            get { return "{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio;
        }
    }

    public class SoundCloudPlaylistId : IExternalId
    {
        public string Name
        {
            get { return "SoundCloud"; }
        }

        public string Key
        {
            get { return "scplaylist"; }
        }

        public string UrlFormatString
        {
            get { return null; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is MusicAlbum;
        }
    }

    public class SoundCloudPlaylistLink : IExternalId
    {
        public string Name
        {
            get { return "Playlist on SoundCloud"; }
        }

        public string Key
        {
            get { return "scplaylistlink"; }
        }

        public string UrlFormatString
        {
            get { return "{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is MusicAlbum;
        }
    }

    public class SoundCloudPurchaseLink : IExternalId
    {
        public string Name
        {
            get { return "Purchase"; }
        }

        public string Key
        {
            get { return "scpurchaselink"; }
        }

        public string UrlFormatString
        {
            get { return "{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio;
        }
    }
    public class SoundCloudDownloadTrackLink : IExternalId
    {
        public string Name
        {
            get { return "Download Track"; }
        }

        public string Key
        {
            get { return "scdownloadtracklink"; }
        }

        public string UrlFormatString
        {
            get { return "{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio;
        }
    }
}
