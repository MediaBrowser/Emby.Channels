namespace MediaBrowser.Channels.HockeyStreams.StreamsApi
{
    public class HighlightsObject
    {
        public string Id { get; set; }
        public string Date { get; set; }
        public string Event { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public string LowQualitySrc { get; set; }
        public string MedQualitySrc { get; set; }
        public string HighQualitySrc { get; set; }
        public string HomeSrc { get; set; }
        public string AwaySrc { get; set; }
    }
}
