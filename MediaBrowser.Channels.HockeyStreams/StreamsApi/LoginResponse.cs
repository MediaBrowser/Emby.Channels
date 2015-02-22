namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    public class LoginResponse : BaseStreamsResponse
    {
        public string Uid { get; set; }
        public string Username { get; set; }
        public string FavTeam { get; set; }
        public Membership Type { get; set; }
        public string Token { get; set; }
    }
}
