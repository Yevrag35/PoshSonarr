namespace MG.Sonarr.Next.Tests.Infrastructure;

/// <summary>
/// Collects what a test's probes report, in the order they report it, from any thread.
/// </summary>
/// <remarks>
/// The type is public because the test cmdlets take it as a parameter.
/// </remarks>
public sealed class ProbeLog
{
	private readonly List<string> _entries = [];
	private readonly Lock _lock = new();

	/// <summary>
	/// Adds an entry to the log.
	/// </summary>
	/// <param name="entry">The entry.</param>
	internal void Add(string entry)
	{
		lock (_lock)
		{
			_entries.Add(entry);
		}
	}

	/// <summary>
	/// Copies the log's entries.
	/// </summary>
	/// <returns>The entries so far, in the order they were added.</returns>
	internal string[] Snapshot()
	{
		lock (_lock)
		{
			return [.. _entries];
		}
	}
}

/// <summary>
/// A singleton of a connection's provider, which logs <c>Connection &lt;name&gt; disposed</c> when the provider is
/// disposed.
/// </summary>
internal sealed class ConnectionProbe : IDisposable
{
	private readonly ProbeLog _log;

	/// <summary>
	/// Gets the name of the <see cref="FakeSonarr"/> whose connection holds the probe.
	/// </summary>
	/// <value>The instance's name.</value>
	internal string Name { get; }

	internal ConnectionProbe(string name, ProbeLog log)
	{
		this.Name = name;
		_log = log;
	}

	public void Dispose()
	{
		_log.Add($"Connection {this.Name} disposed");
	}
}

/// <summary>
/// A scoped service of a connection's provider, which logs <c>Scope &lt;name&gt; disposed</c> when its scope is
/// disposed.
/// </summary>
internal sealed class ScopeProbe : IDisposable
{
	private readonly ProbeLog _log;
	private readonly string _name;

	internal ScopeProbe(string name, ProbeLog log)
	{
		_name = name;
		_log = log;
	}

	public void Dispose()
	{
		_log.Add($"Scope {_name} disposed");
	}
}

/// <summary>
/// Holds a cmdlet in its <c>Begin</c> step until the test releases it.
/// </summary>
/// <remarks>
/// The type is public because the test cmdlets take it as a parameter.
/// </remarks>
public sealed class ProbeGate : IDisposable
{
	private static readonly TimeSpan s_limit = TimeSpan.FromSeconds(30);

	private readonly ManualResetEventSlim _entered = new();
	private readonly ManualResetEventSlim _released = new();
	private readonly CancellationToken _token;

	/// <summary>
	/// Initializes a new <see cref="ProbeGate"/> instance.
	/// </summary>
	/// <param name="token">
	/// The test's token, which ends the cmdlet's wait when the test is canceled, since the cmdlet's thread doesn't see the
	/// test's context.
	/// </param>
	internal ProbeGate(CancellationToken token)
	{
		_token = token;
	}

	/// <summary>
	/// Signals that the cmdlet has reached the gate, and waits until the test releases it.
	/// </summary>
	/// <exception cref="TimeoutException">Thrown when the test doesn't release the gate within 30 seconds.</exception>
	internal void EnterAndWait()
	{
		_entered.Set();
		if (!_released.Wait(s_limit, _token))
		{
			throw new TimeoutException("The test never released the gate.");
		}
	}

	/// <summary>
	/// Releases the cmdlet that waits at the gate.
	/// </summary>
	internal void Release()
	{
		_released.Set();
	}

	/// <summary>
	/// Waits until the cmdlet reaches the gate.
	/// </summary>
	/// <returns><see langword="true"/> if the cmdlet reached it within 30 seconds; otherwise, <see langword="false"/>.</returns>
	internal bool WaitForEntry()
	{
		return _entered.Wait(s_limit, _token);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		_entered.Dispose();
		_released.Dispose();
	}
}
