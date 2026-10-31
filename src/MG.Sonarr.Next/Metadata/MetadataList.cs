using MG.Sonarr.Next.Json;

namespace MG.Sonarr.Next.Metadata;

/// <summary>
/// Provides the factory method that creates a <see cref="MetadataList{T}"/>, which collection expressions also use.
/// </summary>
public static class MetadataList
{
	/// <summary>
	/// Creates a list that holds copies of the specified values.
	/// </summary>
	/// <typeparam name="T">The type of the list's elements.</typeparam>
	/// <param name="values">The values to put in the list, in order.</param>
	/// <returns>A new <see cref="MetadataList{T}"/> that holds <paramref name="values"/>.</returns>
	[DebuggerStepThrough]
	public static MetadataList<T> Create<T>(params ReadOnlySpan<T> values) where T : IComparable<T>, IJsonMetadataTaggable
	{
		return new(values);
	}
}
