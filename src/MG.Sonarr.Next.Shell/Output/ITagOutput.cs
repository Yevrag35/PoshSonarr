using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models;

namespace MG.Sonarr.Next.Shell.Output;

/// <summary>
/// Represents the properties of a tagged object that has an ID, which the output interfaces of such objects build on.
/// </summary>
/// <remarks>
/// No model implements the interface. It names, for a cmdlet's <see cref="OutputTypeAttribute"/>, the properties that
/// the objects that the cmdlet writes have.
/// </remarks>
public interface IIdTagOuptut : IHasId, IJsonSonarrMetadata
{
	/// <summary>
	/// Gets the IDs of the object's tags.
	/// </summary>
	/// <value>The IDs of the tags that the object has, in ascending order.</value>
	SortedSet<int> Tags { get; }
}
/// <summary>
/// Represents the properties of a tagged object, which the output interfaces of such objects build on.
/// </summary>
/// <remarks>
/// No model implements the interface. It names, for a cmdlet's <see cref="OutputTypeAttribute"/>, the properties that
/// the objects that the cmdlet writes have, as <see cref="IDownloadClientOutput"/> does for download clients.
/// </remarks>
public interface ITagOutput : IJsonSonarrMetadata
{
	/// <summary>
	/// Gets the IDs of the object's tags.
	/// </summary>
	/// <value>The IDs of the tags that the object has, in ascending order.</value>
	SortedSet<int> Tags { get; }
}
