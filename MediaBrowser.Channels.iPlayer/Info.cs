using System;
using System.Collections.Generic;

namespace MediaBrowser.Channels.iPlayer
{
    public class BBCChannel
    {
        public String title { get; set; }
        public String thumb { get; set; }
        public String id { get; set; }

        public BBCChannel(String t, String image, String i)
        {
            title = t;
            thumb = image;
            id = i;
        }

        public String highlights_url()
        {
            return "http://feeds.bbc.co.uk/iplayer/" + id + "/highlights";
        }

        public String popular_url()
        {
            return "http://feeds.bbc.co.uk/iplayer/" + id + "/popular";
        }
    }

    public class BBCCategory
    {
        public String title { get; set; }
        public List<BBCCategory> subCategories { get; set; }
        public String id { get; set; }

        public BBCCategory(String t, String[] subCats = null)
        {
            title = t;
            subCategories = new List<BBCCategory>();

            if (subCats != null)
                foreach (var sub in subCats)
                    subCategories.Add(new BBCCategory(sub));

            id = slugify(t);
        }

        public String highlights_url()
        {
            return "http://feeds.bbc.co.uk/iplayer/" + id + "/highlights";
        }

        public String popular_url()
        {
            return "http://feeds.bbc.co.uk/iplayer/" + id + "/popular";
        }
        public String subcategory_url(String subID)
        {
            return "http://feeds.bbc.co.uk/iplayer/" + id + "/" + subID + "/list";
        }

        private String slugify(String title)
        {
            title = title.ToLower();
            title = title.Replace("&", "and");
            title = title.Replace(" ", "_");

            title = title.Replace("'", "").Replace("-", "").Replace(", ", "").Replace("!", "");
            return title;
        }
    }


}
