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
            get { return "SoundCloud User"; }
        }

        public string Key
        {
            get { return "scuser"; }
        }

        public string UrlFormatString
        {
            get { return "/soundcloud/resolveuser/{0}"; }
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
            get { return "SoundCloud Track"; }
        }

        public string Key
        {
            get { return "sctrack"; }
        }

        public string UrlFormatString
        {
            get { return "/soundcloud/resolvetrack/{0}"; }
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
            get { return "SoundCloud Playlist"; }
        }

        public string Key
        {
            get { return "scplaylist"; }
        }

        public string UrlFormatString
        {
            get { return "/soundcloud/resolveplaylist/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is MusicAlbum;
        }
    }
}
