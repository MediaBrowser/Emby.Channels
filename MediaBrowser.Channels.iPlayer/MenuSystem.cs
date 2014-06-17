using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Channels;

namespace MediaBrowser.Channels.iPlayer
{
    internal class MenuSystem
    {
        private String feedURL = "http://feeds.bbc.co.uk";


        public async Task<ChannelItemResult> GetMainMenu(CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>
            {
                CreateMenu("Highlights", "video", feedURL + "/iplayer/highlights/tv"),
                CreateMenu("Most Popular", "video", feedURL + "/iplayer/popular/tv"),
                CreateMenu("TV Channels", "tvChannels", ""),
                CreateMenu("Categories", "categories", ""),
                CreateMenu("Formats", "formats", ""),
                CreateMenu("A-Z", "a-z", "")
            };

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        public async Task<ChannelItemResult> GetTVChannels(CancellationToken cancellationToken)
        {
            var tvChannels = new List<BBCChannel>
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

            // Add more items here.
            var items = new List<ChannelItemInfo>();

            foreach (var c in tvChannels)
            {
                items.Add(CreateMenu(c.title, "channel", c.id, c.thumb));
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        public async Task<ChannelItemResult> GetAToZ(CancellationToken cancellationToken)
        {
            var letters = new[]
            {
                "a", "b", "c", "d", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v",
                "w", "x", "y", "z"
            };
            // Add more items here.
            var items = new List<ChannelItemInfo>();

            foreach (var l in letters)
            {
                items.Add(CreateMenu(l.ToUpper(), "video", feedURL + "/iplayer/atoz/" + l + "/list/tv"));
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        public async Task<ChannelItemResult> GetCategories(String channelID, String thumb, CancellationToken cancellationToken)
        {
            var categories = new List<BBCCategory>
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

            // Add more items here.
            var items = new List<ChannelItemInfo>();

            foreach (var c in categories)
            {
                items.Add(CreateMenu(c.title, "category", c.id + "_" + channelID, thumb));
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        public async Task<ChannelItemResult> GetCategory(String categoryID, String channelID, CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>();
            var categories = new List<BBCCategory>
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

            var category = categories.Find(i => i.id == categoryID);

            if (channelID != "")
            {
                // return videos
            }
            else
            {
                items.Add(CreateMenu(category.title + " Highlights", "video", category.highlights_url()));
                items.Add(CreateMenu(category.title + " Popular", "video", category.popular_url()));
                items.Add(CreateMenu("All programmes", "video", feedURL + "/iplayer/highlights/tv"));

                foreach (var subCat in category.subCategories)
                {
                    items.Add(CreateMenu(subCat.title, "video", category.subcategory_url(subCat.id)));
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }



        // Create Menu Entry
        private ChannelItemInfo CreateMenu(String title, String menu, String id, String thumb = "")
        {
            return new ChannelItemInfo
            {
                Name = title,
                Type = ChannelItemType.Folder,
                Id = menu + "_" + id,
                ImageUrl = thumb
            };
        }

        
    }
}
