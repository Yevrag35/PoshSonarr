using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MG.Sonarr.Next.Generators;

/// <summary>Records a source position as plain values, so the incremental cache holds no syntax tree.</summary>
/// <remarks>
/// The record compares by value: two positions are equal when their file paths and both of their spans are.
/// </remarks>
/// <param name="FilePath">The path of the file the position lives in.</param>
/// <param name="TextSpan">The character span within that file.</param>
/// <param name="LineSpan">The line and column span within that file.</param>
internal sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
	/// <summary>Rebuilds the <see cref="Location"/> this position was taken from.</summary>
	/// <returns>The reconstructed location.</returns>
	public Location ToLocation()
	{
		return Location.Create(this.FilePath, this.TextSpan, this.LineSpan);
	}

	/// <summary>Captures the position of a syntax node.</summary>
	/// <param name="node">The node to take the position of.</param>
	/// <returns>The captured position, or <see langword="null"/> when the node has no source position.</returns>
	public static LocationInfo? From(SyntaxNode? node)
	{
		return From(node?.GetLocation());
	}

	/// <summary>Captures the position an attribute was applied at.</summary>
	/// <param name="reference">The attribute's syntax reference.</param>
	/// <returns>The captured position, or <see langword="null"/> when the attribute has no source position.</returns>
	public static LocationInfo? From(SyntaxReference? reference)
	{
		return reference is null ? null : From(Location.Create(reference.SyntaxTree, reference.Span));
	}

	/// <summary>Captures the position of a location that lives in source.</summary>
	/// <param name="location">The location to take the position of.</param>
	/// <returns>The captured position, or <see langword="null"/> when <paramref name="location"/> is not in a syntax tree.</returns>
	private static LocationInfo? From(Location? location)
	{
		return location?.SourceTree is null
			? null
			: new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
	}
}
