using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes;

/// <summary>
/// Reads the episode file IDs from the pipeline input of the cmdlets that act on episode files.
/// </summary>
/// <remarks>
/// An episode or a calendar entry without a file pipes an <see cref="IEpisodeFilePipeable.EpisodeFileId"/> of 0, which
/// passes the cmdlets' validation so that it can be skipped with a warning instead of failing to bind.
/// </remarks>
internal static class EpisodeFileInput
{
	/// <summary>
	/// Adds the file ID of each input that has a file to a set, and writes a warning for each input that has none.
	/// </summary>
	/// <param name="cmdlet">The cmdlet that writes the warnings.</param>
	/// <param name="inputs">The objects that the cmdlet received.</param>
	/// <param name="fileIds">The set that the file IDs are added to.</param>
	internal static void AddFileIds(Cmdlet cmdlet, ReadOnlySpan<IEpisodeFilePipeable> inputs, ISet<int> fileIds)
	{
		foreach (IEpisodeFilePipeable input in inputs)
		{
			if (input.EpisodeFileId > 0)
			{
				fileIds.Add(input.EpisodeFileId);
			}
			else
			{
				cmdlet.WriteWarning(GetNoFileMessage(input));
			}
		}
	}

	private static string GetNoFileMessage(IEpisodeFilePipeable input)
	{
		return input switch
		{
			IHasId episode and ISeriesPipeable series
				=> $"Episode {episode.Id} of series {series.SeriesId} has no file. It will be ignored.",
			IHasId episode => $"Episode {episode.Id} has no file. It will be ignored.",
			_ => "An episode without a file was passed. It will be ignored.",
		};
	}
}
