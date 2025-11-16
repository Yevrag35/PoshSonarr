using MG.Sonarr.Next.Json;

namespace MG.Sonarr.Next.Metadata;

/// <summary>
/// Provides static methods for creating instances of the generic <see cref="MetadataList{T}"/> class.
/// </summary>
public static class MetadataList
{
	/// <summary>
	/// Creates a new <see cref="MetadataList{T}"/> containing the specified values.
	/// </summary>
	/// <typeparam name="T">The type of elements in the list. Must implement <see cref="IComparable{T}"/> and <see cref="IJsonMetadataTaggable"/>.</typeparam>
	/// <param name="values">A read-only span of values to include in the new <see cref="MetadataList{T}"/>.</param>
	/// <returns>A <see cref="MetadataList{T}"/> instance containing the provided values.</returns>
	[DebuggerStepThrough]
	public static MetadataList<T> Create<T>(params ReadOnlySpan<T> values) where T : IComparable<T>, IJsonMetadataTaggable
	{
		return new(values);
	}
}
