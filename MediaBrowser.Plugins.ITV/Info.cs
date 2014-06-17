using System.Collections.Generic;

namespace MediaBrowser.Plugins.ITV
{
    class TVChannel
    {
        public string name;
        public string fname;
        public string thumb;

        public TVChannel(string n, string fn, string t)
        {
            name = n;
            fname = fn;
            thumb = t;
        }
    }

    class Genre
    {
        public string name;
        public string fname;

        public Genre(string n, string fn)
        {
            name = n;
            fname = fn;
        }
    }

    class Data
    {
        public List<Genre> Genres { get; set; }
        public List<TVChannel> TVChannel { get; set; }

        public Data()
        {
            Genres = new List<Genre>
            {
                new Genre("Children","children"),
		        new Genre("Comedy","comedy"),
		        new Genre("Drama & Soaps","drama-soaps"),
		        new Genre("Entertainment","entertainment"),
		        new Genre("Factual","factual"),
		        new Genre("Films","films"),
		        new Genre("Lifestyle","lifestyle"),
		        new Genre("Music","music"),
		        new Genre("News & Weather","news-weather"),
		        new Genre("Sport","sport"),
            };

            TVChannel = new List<TVChannel>
            {
                // TODO : Move Images to github
                new TVChannel("ITV","itv", "https://raw.githubusercontent.com/snazy2000/MediaBrowser.Channels/5b58af81eea61acd4e72d9d5c045cc923cfbdda3/MediaBrowser.Plugins.ITV/Images/itv.png"),
                new TVChannel("ITV 2","itv2", "https://raw.githubusercontent.com/snazy2000/MediaBrowser.Channels/5b58af81eea61acd4e72d9d5c045cc923cfbdda3/MediaBrowser.Plugins.ITV/Images/itv2.png"),
                new TVChannel("ITV 3","itv3", "https://raw.githubusercontent.com/snazy2000/MediaBrowser.Channels/5b58af81eea61acd4e72d9d5c045cc923cfbdda3/MediaBrowser.Plugins.ITV/Images/itv3.png"),
                new TVChannel("ITV 4","itv4", "https://raw.githubusercontent.com/snazy2000/MediaBrowser.Channels/5b58af81eea61acd4e72d9d5c045cc923cfbdda3/MediaBrowser.Plugins.ITV/Images/itv4.png"),
                new TVChannel("CITV","citv", "https://raw.githubusercontent.com/snazy2000/MediaBrowser.Channels/5b58af81eea61acd4e72d9d5c045cc923cfbdda3/MediaBrowser.Plugins.ITV/Images/citv.png")
            };
        }
    }

}
