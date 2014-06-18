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

    class Data
    {
        public List<BBCCategory> Categories { get; set; }
        public List<BBCChannel> Channels { get; set; }

        public Data()
        {
            Categories = new List<BBCCategory>
            {
                new BBCCategory("Children's", new[] {"Animation", "Drama", "Entertainment & Comedy", "Factual", "Games & Quizzes", "Music", "Other"}),
                new BBCCategory("Comedy", new[] {"Music", "Satire", "Sitcoms", "Sketch", "Spoof", "Standup", "Other"}),
                new BBCCategory("Drama", new[] {"Action & Adventure", "Biographical", "Classic & Period", "Crime", "Historical", "Horror & Supernatural", "Legal & Courtroom", "Medical", "Musical", "Psychological", "Relationships & Romance", "SciFi & Fantasy", "Soaps", "Thriller", "War & Disaster", "Other"}),
                new BBCCategory("Entertainment", new[] { "Discussion & Talk Shows", "Games & Quizzes", "Makeovers", "Phone-ins", "Reality", "Talent Shows", "Variety Shows", "Other"}),
                new BBCCategory("Factual", new[] { "Antiques", "Arts, Culture & the Media", "Beauty & Style", "Cars & Motors", "Cinema", "Consumer", "Crime & Justice", "Disability", "Families & Relationships", "Food & Drink", "Health & Wellbeing", "History", "Homes & Gardens", "Life Stories", "Money", "Pets & Animals", "Politics", "Science & Nature", "Travel", "Other"}),
                new BBCCategory("Learning", new[] {"Pre-School", "5-11", "Adult", "Other" }),
                new BBCCategory("Music", new[] {"Classic Pop & Rock", "Classical", "Country", "Dance & Electronica", "Desi", "Easy Listening, Soundtracks & Musicals", "Folk", "Hip Hop, R'n'B & Dancehall", "Jazz & Blues", "Pop & Chart", "Rock & Indie", "Soul & Reggae", "World", "Other" }),
                new BBCCategory("Sport", new[] {"Boxing", "Cricket", "Cycling", "Equestrian", "Football", "Formula One", "Golf", "Horse Racing", "Motorsport", "Olympics", "Rugby League", "Rugby Union", "Tennis", "Other" })
            };

            Channels = new List<BBCChannel>
            {
                new BBCChannel("BBC One", "bbc_one", "bbcone"),
                new BBCChannel("BBC Two", "bbc_two", "bbctwo"),
                new BBCChannel("BBC Three", "bbc_three", "bbcthree"),
                new BBCChannel("BBC Four", "bbc_four", "bbcfour"),
                new BBCChannel("CBBC", "cbbc", "cbbc"),
                new BBCChannel("CBeebies", "cbeebies_1", "cbeebies"),
                new BBCChannel("BBC News Channel", "bbc_news24", "bbcnews"),
                new BBCChannel("BBC Parliament", "bbc_parliament_1", "bbcparliament"),
                new BBCChannel("BBC HD", "bbc_hd_1", "bbchd"),
                new BBCChannel("BBC Alba", "bbc_alba", "bbcalba")
            };
        }

    }
    


}
