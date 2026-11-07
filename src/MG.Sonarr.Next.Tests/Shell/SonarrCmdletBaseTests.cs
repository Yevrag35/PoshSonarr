using System.Collections.ObjectModel;

namespace MG.Sonarr.Next.Tests.Shell;

/// <summary>
/// Tests the base classes that every API cmdlet runs on, through test cmdlets that log what they do.
/// </summary>
[Collection(ConnectionCollection.Name)]
public sealed class SonarrCmdletBaseTests
{
	/// <summary>
	/// O15: a cmdlet's error goes where <c>-ErrorAction</c> sends it when it happens, and the cmdlet's one scope is
	/// disposed however the cmdlet ends. An exception in <c>Begin</c> ends the cmdlet with its error, so neither
	/// <c>Process</c> nor <c>End</c> runs, one in <c>Process</c> writes its error at once and the cmdlet goes on to the
	/// next input, and one in <c>End</c> writes its error, while under <c>-ErrorAction Stop</c> the statement ends at the
	/// step that threw.
	/// </summary>
	/// <param name="throwIn">The step that throws, for the input 2 when it's <c>Process</c>.</param>
	/// <param name="errorAction">The cmdlet's <c>-ErrorAction</c>.</param>
	/// <param name="expected">
	/// The steps that run, in order, with each error that's written to the error stream as <c>Error: &lt;message&gt;</c>.
	/// </param>
	[Theory]
	[MemberData(nameof(Throws))]
	public void O15_WritesEachErrorWhereItHappensAndStopsWhereItShould(ProbeStep throwIn, ActionPreference errorAction, string[] expected)
	{
		ProbeLog log = new();
		using PowerShell powerShell = PowerShellHost.Create();
		powerShell.Connect(FakeSonarr.Ok("probe", log));

		try
		{
			_ = powerShell.Prepare(
				"Test-Probe",
				("Log", log),
				("ThrowIn", throwIn),
				("ThrowFor", 2),
				("ErrorAction", errorAction));

			powerShell.Streams.Error.DataAdded += (sender, e) =>
				log.Add("Error: " + ((PSDataCollection<ErrorRecord>)sender!)[e.Index].Exception.Message);

			string failure = throwIn switch
			{
				ProbeStep.Begin => "Begin failed",
				ProbeStep.Process => "Process failed for 2",
				_ => "End failed",
			};

			if (errorAction == ActionPreference.Stop)
			{
				RuntimeException stopped = Assert.ThrowsAny<RuntimeException>(() => powerShell.Invoke(new object[] { 1, 2, 3 }));
				Assert.Equal(failure, stopped.ErrorRecord.Exception.Message);

				// Under Stop, the error ends the statement, wherever PowerShell records it.
				Assert.Equal(expected, log.Snapshot().Where(entry => !entry.StartsWith("Error: ", StringComparison.Ordinal)));
			}
			else
			{
				_ = powerShell.Invoke(new object[] { 1, 2, 3 });
				Assert.Equal(expected, log.Snapshot());
			}
		}
		finally
		{
			powerShell.Disconnect();
		}
	}

	public static TheoryData<ProbeStep, ActionPreference, string[]> Throws => new()
	{
		{
			ProbeStep.Begin,
			ActionPreference.Continue,
			["Begin", "Error: Begin failed", "Scope probe disposed"]
		},
		{
			ProbeStep.Process,
			ActionPreference.Continue,
			["Begin", "Process 1", "Process 2", "Error: Process failed for 2", "Process 3", "End", "Scope probe disposed"]
		},
		{
			ProbeStep.End,
			ActionPreference.Continue,
			["Begin", "Process 1", "Process 2", "Process 3", "End", "Error: End failed", "Scope probe disposed"]
		},
		{
			ProbeStep.Begin,
			ActionPreference.Stop,
			["Begin", "Scope probe disposed"]
		},
		{
			ProbeStep.Process,
			ActionPreference.Stop,
			["Begin", "Process 1", "Process 2", "Scope probe disposed"]
		},
		{
			ProbeStep.End,
			ActionPreference.Stop,
			["Begin", "Process 1", "Process 2", "Process 3", "End", "Scope probe disposed"]
		},
	};

	/// <summary>
	/// B116: a cmdlet takes its error, verbose, and debug preferences from the caller's preference variables when the
	/// common parameter isn't bound, whether a variable holds an <see cref="ActionPreference"/> or the string that a
	/// function or a script block assigned, such as <c>'Continue'</c>, which converts as PowerShell converts it, in any
	/// case. A bound common parameter wins over the variable.
	/// </summary>
	/// <param name="script">The script that calls <c>Test-Preference</c>.</param>
	/// <param name="expected">The error, verbose, and debug preferences that the cmdlet runs with.</param>
	[Theory]
	[MemberData(nameof(Preferences))]
	public void B116_TakesItsPreferencesFromTheCallersVariables(string script, string expected)
	{
		using PowerShell powerShell = PowerShellHost.Create();
		powerShell.Connect(FakeSonarr.Ok("preferences", new ProbeLog()));

		try
		{
			powerShell.Commands.Clear();
			Collection<PSObject> output = powerShell.AddScript(script).Invoke();

			Assert.Empty(powerShell.Streams.Error);
			Assert.Equal(expected, Assert.Single(output).BaseObject);
		}
		finally
		{
			powerShell.Disconnect();
		}
	}

	public static TheoryData<string, string> Preferences => new()
	{
		{
			"Test-Preference",
			"Continue SilentlyContinue SilentlyContinue"
		},
		{
			"function Invoke-Test { $ErrorActionPreference = 'Stop'; $VerbosePreference = 'Continue'; $DebugPreference = 'Inquire'; Test-Preference }; Invoke-Test",
			"Stop Continue Inquire"
		},
		{
			"& { $ErrorActionPreference = 'silentlycontinue'; $VerbosePreference = 'INQUIRE'; $DebugPreference = 'continue'; Test-Preference }",
			"SilentlyContinue Inquire Continue"
		},
		{
			"function Invoke-Test { $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop; $VerbosePreference = [System.Management.Automation.ActionPreference]::Continue; $DebugPreference = [System.Management.Automation.ActionPreference]::Inquire; Test-Preference }; Invoke-Test",
			"Stop Continue Inquire"
		},
		{
			"function Invoke-Test { $ErrorActionPreference = 'Stop'; $VerbosePreference = 'Continue'; $DebugPreference = 'Continue'; Test-Preference -ErrorAction Ignore -Verbose:$false -Debug:$false }; Invoke-Test",
			"Ignore SilentlyContinue SilentlyContinue"
		},
	};

	/// <summary>
	/// B118: stopping the pipeline, as Ctrl+C does, ends the request that a cmdlet has in flight, which then reports
	/// <see cref="ErrorCategory.OperationStopped"/>. <c>StopProcessing</c> only signals: it runs the cmdlet's <c>Stop</c>
	/// and leaves its scope alone, so the scope still serves <c>Process</c> after the stop, and the pipeline thread
	/// disposes it once the pipeline ends.
	/// </summary>
	[Fact]
	public async Task B118_StoppingThePipelineEndsTheRequestInFlight()
	{
		ProbeLog log = new();
		using ProbeGate gate = new(TestContext.Current.CancellationToken);
		using CancellationTokenSource release = new();
		TaskCompletionSource sent = new(TaskCreationOptions.RunContinuationsAsynchronously);

		// The request ends only when its token is canceled, or when the test releases it.
		FakeSonarr sonarr = new("stalling", log, async (request, token) =>
		{
			_ = sent.TrySetResult();
			using CancellationTokenSource either = CancellationTokenSource.CreateLinkedTokenSource(token, release.Token);
			await Task.Delay(Timeout.Infinite, either.Token).ConfigureAwait(false);
			throw new UnreachableException();
		});

		using PowerShell other = PowerShellHost.Create();
		using PowerShell running = PowerShellHost.Create();
		other.Connect(sonarr);

		try
		{
			Task<PSDataCollection<PSObject>> invoking = running.Prepare("Test-Request", ("Log", log), ("Gate", gate)).InvokeAsync();
			Task first = await Task.WhenAny(sent.Task, invoking).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
			if (first != sent.Task)
			{
				// The error stream's enumerator blocks until the pipeline ends, which it has here.
				Assert.Fail($"The cmdlet ended before its request reached the server: {string.Join("; ", running.Streams.Error)}");
			}

			// A stop that leaves the request alone waits for it, and so for the release in the finally block.
			await running.StopAsync(null, null).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
			_ = await Assert.ThrowsAnyAsync<PipelineStoppedException>(() => invoking);

			Assert.Equal(["Stop", "Request ended: OperationStopped", "Scope open", "Scope stalling disposed"], log.Snapshot());
		}
		finally
		{
			await release.CancelAsync();
			gate.Release();
			other.Disconnect();
		}
	}
}
