namespace MG.Sonarr.Next.Shell.Models.Series;

[DebuggerStepThrough]
public abstract record SeriesAddOptions
{
	public abstract bool IgnoreEpisodesWithFiles { get; }
	public abstract bool IgnoreEpisodesWithoutFiles { get; }
	public abstract bool SearchForMissingEpisodes { get; }

	public static readonly SeriesAddOptions Default = new DefaultInstance();

	private sealed record DefaultInstance : SeriesAddOptions
	{
		public override bool IgnoreEpisodesWithFiles => true;
		public override bool IgnoreEpisodesWithoutFiles => true;
		public override bool SearchForMissingEpisodes => false;
	}
}
