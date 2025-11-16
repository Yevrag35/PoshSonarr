using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Models.Series;

namespace MG.Sonarr.Next.Shell.Components;

internal sealed class ManualImportEdit
{
	[MemberNotNullWhen(true, nameof(Episode))]
	internal bool HasEpisode { get; private set; }

	[MemberNotNullWhen(true, nameof(Quality))]
	internal bool HasQuality { get; private set; }

	[MemberNotNullWhen(true, nameof(Series))]
	internal bool HasSeries { get; private set; }

	internal EpisodeObject? Episode
	{
		get;
		set
		{
			field = value;
			this.HasEpisode = value is not null;
		}
	}
	internal QualityRevisionObject? Quality
	{
		get;
		set
		{
			field = value;
			this.HasQuality = value is not null;
		}
	}
	internal SeriesObject? Series
	{
		get;
		set
		{
			field = value;
			this.HasSeries = value is not null;
		}
	}

	public ManualImportEdit()
	{
	}

	internal void EditImport(ManualImportObject importObj)
	{
		ArgumentNullException.ThrowIfNull(importObj);

		if (this.HasEpisode)
		{
			importObj.Episodes.Add(this.Episode);
		}

		if (this.HasQuality)
		{
			importObj.Quality = this.Quality;
		}

		if (this.HasSeries)
		{
			importObj.Series = this.Series;
		}
	}
}

