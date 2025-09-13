using MG.Sonarr.Next.Shell.Models.Series;

namespace MG.Sonarr.Next.Shell.Internal;

public enum SeriesAddIgnoreAction
{
    Default,
    IgnoreWithEpisodes,
    IgnoreWithoutEpisodes,
}

[DebuggerStepThrough]
internal sealed record EditableSeriesAddOptions : SeriesAddOptions
{
    public override bool IgnoreEpisodesWithFiles => this.IgnoreEpsWithFiles;
    public override bool IgnoreEpisodesWithoutFiles => this.IgnoreEpsWithoutFiles;
    public override bool SearchForMissingEpisodes => this.SearchForMissingEps;

    internal bool IgnoreEpsWithFiles { get; set; } = true;
    internal bool IgnoreEpsWithoutFiles { get; set; } = true;
    internal bool SearchForMissingEps { get; set; }
}
