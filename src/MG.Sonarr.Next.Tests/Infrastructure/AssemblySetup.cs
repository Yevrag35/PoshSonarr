namespace MG.Sonarr.Next.Tests.Infrastructure;

/// <summary>
/// Prepares the test process before any test runs.
/// </summary>
internal static class AssemblySetup
{
	/// <summary>
	/// Replaces the process's trace listeners with a <see cref="ThrowingTraceListener"/>.
	/// </summary>
	/// <remarks>
	/// A failed <see cref="Debug.Assert(bool)"/> ends a process that has no debugger attached, which would take every
	/// test down with it. With the listener in place, the assert throws instead, so the test that reached it fails. The
	/// module initializer runs before any code in this assembly, so before any test.
	/// </remarks>
	[ModuleInitializer]
	internal static void Initialize()
	{
		Trace.Listeners.Clear();
		_ = Trace.Listeners.Add(new ThrowingTraceListener());
	}
}

/// <summary>
/// Turns a failed <see cref="Debug.Assert(bool)"/> into a <see cref="DebugAssertFailedException"/>.
/// </summary>
internal sealed class ThrowingTraceListener : TraceListener
{
	/// <inheritdoc/>
	/// <exception cref="DebugAssertFailedException">Thrown always.</exception>
	public override void Fail(string? message, string? detailMessage)
	{
		throw new DebugAssertFailedException(message, detailMessage);
	}

	/// <inheritdoc/>
	public override void Write(string? message)
	{
	}

	/// <inheritdoc/>
	public override void WriteLine(string? message)
	{
	}
}

/// <summary>
/// The exception that <see cref="ThrowingTraceListener"/> throws for a failed <see cref="Debug.Assert(bool)"/>.
/// </summary>
internal sealed class DebugAssertFailedException : Exception
{
	/// <summary>
	/// Initializes a new <see cref="DebugAssertFailedException"/> instance with the assert's message and details.
	/// </summary>
	/// <param name="message">The assert's message, or <see langword="null"/>.</param>
	/// <param name="detailMessage">The assert's detailed message, or <see langword="null"/>.</param>
	internal DebugAssertFailedException(string? message, string? detailMessage)
		: base(string.IsNullOrEmpty(detailMessage)
			? $"Debug.Assert failed: {message}"
			: $"Debug.Assert failed: {message} {detailMessage}")
	{
	}
}
