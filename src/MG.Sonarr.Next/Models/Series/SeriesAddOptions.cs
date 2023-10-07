namespace MG.Sonarr.Next.Shell.Models.Series
{
    public sealed class SeriesAddOptions
    {
        public bool IgnoreEpisodesWithFiles { get; set; } = true;
        public bool IgnoreEpisodesWithoutFiles { get; set; } = true;
        public bool SearchForMissingEpisodes { get; set; }
    }
}
