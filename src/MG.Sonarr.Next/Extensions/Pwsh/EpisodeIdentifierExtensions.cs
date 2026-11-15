using MG.Sonarr.Next.Models.Episodes;

namespace MG.Sonarr.Next.Extensions.Pwsh;

/// <summary>
/// Provides extension methods for working with <see cref="IEpisodeIdentifier"/> instances in PowerShell cmdlets.
/// </summary>
internal static class EpisodeIdentifierExtensions
{
	/// <summary>
	/// Determines whether all of the identifiers in the given array are valid, and if not, returns an <see cref="ErrorRecord"/> describing the invalid identifiers.
	/// </summary>
	/// <typeparam name="T">The type of the episode identifier.</typeparam>
	/// <param name="array">The array of episode identifiers to validate.</param>
	extension<T>([NotNullWhen(true)] T[]? array) where T : IEpisodeIdentifier
	{
		/// <summary>
		/// Determines whether all of the identifiers in the given array are valid, and if not, returns an <see cref="ErrorRecord"/> describing the invalid identifiers.
		/// </summary>
		/// <param name="error">The error record describing any invalid identifiers.</param>
		/// <param name="argumentName">The name of the argument.</param>
		/// <returns><see langword="true"/> if all identifiers are valid; otherwise, <see langword="false"/>.</returns>
		internal bool AreAllValid([NotNullWhen(false)] out ErrorRecord? error, [CallerArgumentExpression(nameof(array))] string argumentName = "")
		{
			if (array is null || array.Length == 0)
			{
				error = FromException(new ArgumentException("No identifiers were specified", argumentName.Replace("this.", string.Empty)), array);
				return false;
			}

			List<T>? list = null;
			foreach (T item in array)
			{
				if (!item.IsValid())
				{
					list ??= new(1);
					list.Add(item);
				}
			}

			if (list is { Count: > 0 })
			{
				error = FromException(InvalidEpisodeIdFormatException.FromList(list, null), list);
				return false;
			}
			else
			{
				error = null;
				return true;
			}
		}
	}
	/// <summary>
	/// Filters the given collection of <see cref="EpisodeObject"/> instances to only those that match any of the identifiers in the given array.
	/// </summary>
	/// <typeparam name="T">The type of the episode identifier.</typeparam>
	/// <param name="array">The array of episode identifiers to filter by.</param>

	extension<T>(T[] array) where T : IEpisodeIdentifier
	{
		/// <summary>
		/// Filters the given collection of <see cref="EpisodeObject"/> instances to only those that match any of the identifiers in the given array.
		/// </summary>
		/// <param name="collection">The collection of <see cref="EpisodeObject"/> instances to filter.</param>
		/// <returns>An <see cref="IEnumerable{EpisodeObject}"/> containing only the episodes that match any of the identifiers in the array.</returns>
		internal IEnumerable<EpisodeObject> FilterEpisodes(IEnumerable<EpisodeObject> collection)
		{
			ArgumentNullException.ThrowIfNull(array);
			ArgumentNullException.ThrowIfNull(collection);

			foreach (EpisodeObject episode in collection)
			{
				foreach (T identifier in array)
				{
					if (episode.Matches(identifier))
					{
						yield return episode;
						break;
					}
				}
			}
		}
	}

	/// <summary>
	/// Creates an <see cref="ErrorRecord"/> from the given exception and argument.
	/// </summary>
	/// <param name="exception">The exception to create the error record from.</param>
	/// <param name="argument">The argument that caused the exception.</param>
	/// <returns>An <see cref="ErrorRecord"/> representing the exception and argument.</returns>
	private static ErrorRecord FromException(Exception exception, object? argument)
	{
		return exception.ToRecord(ErrorCategory.InvalidArgument, argument);
	}
}