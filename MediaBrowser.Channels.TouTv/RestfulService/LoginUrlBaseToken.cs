using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Channels.TouTv.RestfulService
{
    [Route("/TouTv/Auth/LoginUrlBase", "GET", Summary = "Gets base URL for login page")]
    public class LoginUrlBaseToken : IReturn<QueryResult<string>>
    {
    }
}
