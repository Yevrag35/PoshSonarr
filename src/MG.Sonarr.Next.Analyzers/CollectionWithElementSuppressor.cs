using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace MG.Sonarr.Next.Analyzers;

/// <summary>
/// Suppresses IDE0028 where its fix writes a collection expression with a <c>with(...)</c> element.
/// </summary>
/// <remarks>
/// <para>
/// IDE0028 offers a collection initializer or a collection expression in place of an object creation, and no
/// editorconfig option sets apart the collection expressions that need <c>with(...)</c>, the element that passes a
/// constructor's arguments, from the ones that don't. The fix writes <c>with(...)</c> when the creation passes exactly
/// one argument, and never proposes a collection expression for a creation that passes two or more, so this suppressor
/// hides the diagnostics that propose a collection expression for a creation with one argument and keeps the rest.
/// </para>
/// <para>
/// One case is hidden although its fix writes no <c>with(...)</c>: when the argument is an <c>int capacity</c> that
/// equals the number of elements that the collection ends up with, the fix drops the argument and writes the elements
/// alone. Telling that case apart would take the analyzer's own matching of the statements that the fix folds in.
/// </para>
/// <para>
/// The suppressor reads details of the code-style analyzers that aren't a public contract. A diagnostic whose fix
/// writes a collection expression carries the <c>UseCollectionExpressionName</c> property, and the object creation is
/// the first additional location of that diagnostic and of each fading copy, which marks a statement that the fix folds
/// into the collection. If an update drops the property, nothing is suppressed.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CollectionWithElementSuppressor : DiagnosticSuppressor
{
	/// <summary>The property that marks an IDE0028 whose fix writes a collection expression.</summary>
	private const string USE_COLLECTION_EXPRESSION = "UseCollectionExpressionName";

	private static readonly SuppressionDescriptor s_rule = new(
		id: "PSNRS001",
		suppressedDiagnosticId: "IDE0028",
		justification: "IDE0028 isn't reported where its fix writes a collection expression with a with(...) element.");

	/// <inheritdoc/>
	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = ImmutableArray.Create(s_rule);

	/// <inheritdoc/>
	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
		foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
		{
			if (ProposesWithElement(diagnostic, context.CancellationToken))
				context.ReportSuppression(Suppression.Create(s_rule, diagnostic));
		}
	}

	/// <summary>Determines whether an IDE0028 proposes a collection expression for a creation with one argument.</summary>
	/// <param name="diagnostic">The IDE0028 to check.</param>
	/// <param name="cancellationToken">The token that cancels the check.</param>
	/// <returns>
	/// <see langword="true"/> if the diagnostic's fix writes a collection expression and its object creation passes
	/// exactly one argument; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool ProposesWithElement(Diagnostic diagnostic, CancellationToken cancellationToken)
	{
		if (!diagnostic.Properties.ContainsKey(USE_COLLECTION_EXPRESSION) || diagnostic.AdditionalLocations.Count == 0)
			return false;

		Location creation = diagnostic.AdditionalLocations[0];
		if (creation.SourceTree is not { } tree)
			return false;

		// An argument spans exactly the creation that it holds, so the tie goes to the innermost node, the creation.
		SyntaxNode node = tree.GetRoot(cancellationToken).FindNode(creation.SourceSpan, getInnermostNodeForTie: true);
		return node is BaseObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 1 };
	}
}
