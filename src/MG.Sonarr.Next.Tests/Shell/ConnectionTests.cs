using MG.Sonarr.Next.Exceptions.Shell;

namespace MG.Sonarr.Next.Tests.Shell;

/// <summary>
/// Tests the module's connection, which every runspace in the process shares.
/// </summary>
[Collection(ConnectionCollection.Name)]
public sealed class ConnectionTests
{
	/// <summary>
	/// B117: a cmdlet that runs while another runspace connects or disconnects finishes on the connection that it started
	/// with. A connection that a later one replaced, or that a disconnect removed, keeps its services until the last
	/// scope on it ends and is disposed then, and a cmdlet that starts with no connection fails with
	/// <see cref="ContextNotSetException"/>.
	/// </summary>
	[Fact]
	public void B117_ACmdletFinishesOnTheConnectionThatItStartedWith()
	{
		ProbeLog log = new();
		using PowerShell other = PowerShellHost.Create();
		using PowerShell running = PowerShellHost.Create();
		using ProbeGate gate = new(TestContext.Current.CancellationToken);

		other.Connect(FakeSonarr.Ok("first", log));
		try
		{
			// The cmdlet takes its scope on the first connection, then waits in Begin before it uses it.
			IAsyncResult pending = running.Prepare("Test-Probe", ("Log", log), ("HoldInBegin", gate)).BeginInvoke();
			Assert.True(gate.WaitForEntry(), "The cmdlet never reached its Begin step.");

			other.Connect(FakeSonarr.Ok("second", log));
			Assert.DoesNotContain("Connection first disposed", log.Snapshot());

			other.Disconnect();
			Assert.Contains("Connection second disposed", log.Snapshot());

			RuntimeException notSet = Assert.ThrowsAny<RuntimeException>(() => other.Run("Test-Probe", ("Log", log)));
			Assert.IsType<ContextNotSetException>(notSet.InnerException ?? notSet.ErrorRecord.Exception);

			gate.Release();
			PSObject output = Assert.Single(running.EndInvoke(pending));

			Assert.Empty(running.Streams.Error);
			Assert.Equal("first", output.BaseObject);
			Assert.Equal(
				["Connection second disposed", "Begin", "Process 0", "End", "Scope first disposed", "Connection first disposed"],
				log.Snapshot());
		}
		finally
		{
			gate.Release();
			other.Disconnect();
		}
	}
}
