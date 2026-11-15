using Microsoft.CodeAnalysis;

namespace MG.Sonarr.Next.Generators;

/// <summary>Carries a diagnostic through the incremental pipeline without rooting a syntax tree.</summary>
/// <remarks>
/// The record compares by value, so a generator's result that holds one equals the result that the next run builds for
/// a declaration that an edit leaves unchanged, and the pipeline reuses its earlier output for it.
/// </remarks>
/// <param name="Descriptor">The rule being reported.</param>
/// <param name="Location">The position to report it at, or <see langword="null"/> for no position.</param>
/// <param name="Argument0">The first message argument.</param>
/// <param name="Argument1">The second message argument, which a message with one placeholder ignores.</param>
internal sealed record DiagnosticInfo(
	DiagnosticDescriptor Descriptor,
	LocationInfo? Location,
	string Argument0,
	string Argument1)
{
	/// <summary>Materializes the reportable diagnostic.</summary>
	/// <returns>The diagnostic to report.</returns>
	public Diagnostic ToDiagnostic()
	{
		return Diagnostic.Create(this.Descriptor, this.Location?.ToLocation(), this.Argument0, this.Argument1);
	}
}
