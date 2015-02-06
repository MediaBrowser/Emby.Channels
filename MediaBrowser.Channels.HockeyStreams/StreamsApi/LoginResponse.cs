namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    internal class LoginResponse : BaseStreamsResponse
    {
        public string Uid { get; set; }
        public string Username { get; set; }
        public string FavTeam { get; set; }
        public Membership Type { get; set; }
        public string Token { get; set; }
    }
}
