using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Models.Qualities;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.ManualImports;

public sealed class ManualImportPost
{
	public required string Path { get; set; }
	public required int SeriesId { get; set; }
	public required int[] EpisodeIds { get; set; }
	public required Dictionary<string, object?> Quality { get; set; }
	public required Dictionary<string, object?>[] Languages { get; set; }
	public required string ReleaseGroup { get; set; }
	public required int IndexerFlags { get; set; }
	public required string ReleaseType { get; set; }
}

public static class ManualImportPostMapper
{
	public static ManualImportPost[] ToManualImportFiles(this MetadataList<ManualImportObject> list)
	{
		ArgumentNullException.ThrowIfNull(list);

		ManualImportPost[] array = new ManualImportPost[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			var obj = list[i];
			array[i] = new()
			{
				EpisodeIds = [.. obj.Episodes.Select(x => x.Id)],
				IndexerFlags = obj.GetValue<int>("IndexerFlags"),
				Languages = obj.GetValue<PSObject[]>(nameof(ManualImportPost.Languages))?.Select(x => x.ToDictionary(nameof(LanguageProfileObject.MetadataTag))).ToArray() ?? [],
				Path = obj.GetValue<string>(nameof(ManualImportPost.Path)) ?? throw new ArgumentException("Path cannot be null."),
				Quality = obj.Quality?.ToDictionary(nameof(QualityRevisionObject.MetadataTag)) ?? throw new ArgumentException("Quality cannot be null."),
				ReleaseGroup = obj.GetValue<string>(nameof(ManualImportPost.ReleaseGroup)) ?? string.Empty,
				ReleaseType = obj.GetValue<string>(nameof(ManualImportPost.ReleaseType)) ?? string.Empty,
				SeriesId = obj.Series?.Id ?? throw new ArgumentException("Series cannot be null."),
			};
		}

		return array;
	}
}

