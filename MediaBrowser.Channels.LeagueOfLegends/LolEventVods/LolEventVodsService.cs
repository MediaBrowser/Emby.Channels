using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Channels.LeagueOfLegends.Twitch;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Channels.LeagueOfLegends.LolEventVods
{
    internal class LolEventVodsService : ApiService
    {
        protected override string BaseUrl
        {
            get { return "http://www.reddit.com/"; }
        }

        public LolEventVodsService(IHttpClient httpClient, IJsonSerializer jsonSerializer)
            : base(httpClient, jsonSerializer)
        { }

        public async Task<Events> GetEvents(int limit, string after, CancellationToken cancellationToken)
        {
            string url = string.IsNullOrEmpty(after)
                ? string.Format("/r/loleventvods/.json?limit={0}", limit)
                : string.Format("/r/loleventvods/.json?limit={0}&after={1}", limit, after);
            var listing = await ExecuteRequest<RedditListing>(url, cancellationToken);
            return TransformListingToEvents(listing);
        }

        public async Task<IEnumerable<Day>> GetEventDays(string eventId, CancellationToken cancellationToken)
        {
            string url = string.Format("/r/loleventvods/comments/{0}/.json", eventId);
            var listing = await ExecuteRequest<List<RedditListing>>(url, cancellationToken);
            return TransformListingToEventContent(listing[0]);
        }

        private Events TransformListingToEvents(RedditListing listing)
        {
            return new Events
            {
                After = listing.Data.After,
                Items = listing.Data.Children.Select(TransformPostToEvent)
            };
        }

        private Event TransformPostToEvent(RedditPost post)
        {
            var data = post.Data;
            var parser = new RedditHtmlParser(data.Selftext_Html);
            return new Event
            {
                CreatedOn = TimeStampToDateTime(data.Created),
                EventId = data.Id,
                ImageUrl = parser.ImageUrl,
                Status = TransformCssClassToStatus(data.Link_Flair_Css_Class, parser),
                Title = data.Title
            };
        }

        private static DateTime TimeStampToDateTime(long timeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return dateTime.AddSeconds(timeStamp);
        }

        private static EventStatus TransformCssClassToStatus(string cssClass, RedditHtmlParser parser)
        {
            switch (cssClass)
            {
                case "ongoing":
                case "twitchongoing":
                    return EventStatus.Ongoing;
                case "finished":
                    return EventStatus.Finished;
                case "featured":
                case "finishedfeatured":
                    return EventStatus.Featured;
                default:
                    if (parser.IsEvent)
                    {
                        return EventStatus.UnknownStatus;
                    }
                    return EventStatus.None;
            }
        }

        private IEnumerable<Day> TransformListingToEventContent(RedditListing listing)
        {
            var html = listing.Data.Children[0].Data.Selftext_Html;
            var parser = new RedditHtmlParser(html);
            var imageUrl = parser.ImageUrl;
            return parser.Tables.Select((table, index) => CreateDay(table, imageUrl, index));
        }

        private Day CreateDay(Table table, string imageUrl, int dayId)
        {
            var fullStream = GetFullStreamVideoLink(table.Title, table.FullStreamUrl);
            return new Day
            {
                DayId = dayId.ToString(CultureInfo.InvariantCulture),
                Title = table.Title,
                ImageUrl = imageUrl,
                Matches = table.Matches,
                FullStream = fullStream
            };
        }

        private static VideoLink GetFullStreamVideoLink(string title, string fullStreamUrl)
        {
            if (string.IsNullOrEmpty(fullStreamUrl))
            {
                return null;
            }
            var twitchUrlParser = new TwitchUrlParser(fullStreamUrl);
            return new VideoLink
            {
                Title = "Full stream: " + title,
                TwitchId = twitchUrlParser.Id,
                TimeStart = twitchUrlParser.TimeStart
            };
        }
    }
}
