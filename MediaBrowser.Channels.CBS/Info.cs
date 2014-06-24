using System.Collections.Generic;

namespace MediaBrowser.Channels.CBS
{
    class Category
    {
        public string name;
        public int id;
        public string thumb;

        public Category(int i, string n, string t)
        {
            name = n;
            id = i;
            thumb = t;
        }
    }

    class Genre
    {
        public string name;
        public string fname;
        public string thumb;

        public Genre(string n, string fn, string t)
        {
            name = n;
            fname = fn;
            thumb = t;
        }
    }

    class Data
    {
        public List<Genre> Genres { get; set; }
        public List<Category> Categories { get; set; }

        public Data()
        {
            Genres = new List<Genre>
            {
                new Genre("Children","children", "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/genres/Children/thumb.jpg"),
		        new Genre("Comedy","comedy", "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/genres/Comedy/thumb.jpg"),
		        new Genre("Drama & Soaps","drama-soaps", "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/genres/Soap/thumb.jpg"),
		        new Genre("Entertainment","entertainment",""),
		        new Genre("Factual","factual", ""),
		        new Genre("Films","films", ""),
		        new Genre("Lifestyle","lifestyle", ""),
		        new Genre("Music","music", "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/genres/Music/thumb.jpg"),
		        new Genre("News & Weather","news-weather", "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/genres/News/thumb.jpg"),
		        new Genre("Sport","sport", "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/genres/Sport/thumb.jpg"),
            };

            Categories = new List<Category>
            {
                new Category(0,"All Current Shows", ""),
                new Category(1,"Primetime", ""),
                new Category(2,"Daytime", ""),
                new Category(3,"Late Night", ""),
                new Category(6,"Movies & Specials", "")

            };
        }
    }

    public class NavigationItemLink
    {
        public string link { get; set; }
        public string title { get; set; }
    }

    public class ShowDatum
    {
        public int id { get; set; }
        public int nav_category_id { get; set; }
        public string title { get; set; }
        public int show_id { get; set; }
        public int device_restriction { get; set; }
        public string tune_in_time { get; set; }
        public string category { get; set; }
        public string type { get; set; }
        public string link { get; set; }
        public int display_order { get; set; }
        public string filepath_show_logo { get; set; }
        public string filepath_show_thumbnail { get; set; }
        public string filepath_nav_logo { get; set; }
        public object created_date { get; set; }
        public object changed_date { get; set; }
        public object live_date { get; set; }
        public string filepath_nav_small_photo { get; set; }
        public string filepath_nav_medium_photo { get; set; }
        public string filepath_grid { get; set; }
        public string filepath_ipad { get; set; }
        public string filepath_list { get; set; }
        public string tvplus_id { get; set; }
        public int? tvplus { get; set; }
        public int seasonNumber { get; set; }
        public string filepath_show_global_nav_logo { get; set; }
        public List<NavigationItemLink> navigationItemLink { get; set; }
        public object assets { get; set; }
    }

    public class ShowResult
    {
        public int total { get; set; }
        public List<ShowDatum> data { get; set; }
    }

    public class ShowList
    {
        public bool success { get; set; }
        public ShowResult result { get; set; }
    }

    public class CategoryThumb
    {
        public string small { get; set; }
        public string large { get; set; }
        public string __invalid_name__640x360 { get; set; }
        public string __invalid_name__640x480 { get; set; }
    }

    public class CategoryDatum
    {
        public string type { get; set; }
        public string title { get; set; }
        public string series_title { get; set; }
        public string label { get; set; }
        public string content_id { get; set; }
        public string airdate { get; set; }
        public string season_number { get; set; }
        public string episode_number { get; set; }
        public string duration { get; set; }
        public string description { get; set; }
        public CategoryThumb thumb { get; set; }
        public string url { get; set; }
        public string amazon_est_url { get; set; }
        public string itunes_est_url { get; set; }
        public string streaming_url { get; set; }
        public string tms_program_id { get; set; }
        public int show_id { get; set; }
        public string asset_type { get; set; }
        public string raw_url { get; set; }
        public string episode_title { get; set; }
        public string status { get; set; }
        public string expiry_date { get; set; }
        public bool url_in_window { get; set; }
    }

    public class CategoryResult
    {
        public int id { get; set; }
        public string title { get; set; }
        public int layout { get; set; }
        public int total { get; set; }
        public int size { get; set; }
        public bool has_full_episode { get; set; }
        public List<CategoryDatum> data { get; set; }
    }

    public class CategoryList
    {
        public bool success { get; set; }
        public CategoryResult result { get; set; }
    }
}
