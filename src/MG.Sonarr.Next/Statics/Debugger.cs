namespace MG.Sonarr.Next;

/// <summary>
/// Provides debug-only assertion helpers.
/// </summary>
public static class Debugger
{
	/// <summary>
	/// Asserts that the specified predicate evaluates to <see langword="true"/>.
	/// </summary>
	/// <param name="predicate">The assertion predicate to evaluate.</param>
	[Conditional("DEBUG")]
	[DebuggerStepThrough]
	public static void Assert(Func<bool> predicate)
	{
		Debug.Assert(predicate());
	}

	/// <summary>
	/// Asserts that the specified predicate evaluates to <see langword="true"/> and reports a message when it fails.
	/// </summary>
	/// <param name="predicate">The assertion predicate to evaluate.</param>
	/// <param name="message">The message to include when the assertion fails.</param>
	[Conditional("DEBUG")]
	[DebuggerStepThrough]
	public static void Assert(Func<bool> predicate, string? message)
	{
		Debug.Assert(predicate(), message);
	}

	/// <summary>
	/// Asserts that the specified predicate evaluates to <see langword="true"/> and reports detailed messages when it fails.
	/// </summary>
	/// <param name="predicate">The assertion predicate to evaluate.</param>
	/// <param name="message">The message to include when the assertion fails.</param>
	/// <param name="detailedMessage">Additional assertion details to include when the assertion fails.</param>
	[Conditional("DEBUG")]
	[DebuggerStepThrough]
	public static void Assert(Func<bool> predicate, string? message, string? detailedMessage)
	{
		Debug.Assert(predicate(), message, detailedMessage);
	}
}

