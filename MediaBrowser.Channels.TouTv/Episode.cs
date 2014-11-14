namespace MediaBrowser.Channels.TouTv
{
    internal class Episode
    {
        public string ShowId { get; private set; }
        public string EpisodeId { get; private set; }

        public Episode(string id)
        {
            var ids = id.Split('-');
            ShowId = ids[0];
            EpisodeId = ids[1];
        }

        public Episode(string showId, string episodeId)
        {
            ShowId = showId;
            EpisodeId = episodeId;
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}", ShowId, EpisodeId);
        }
    }
}
