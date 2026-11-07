namespace MG.Sonarr.Next.Tests.Shell;

/// <summary>
/// Tests the base class that every API cmdlet's steps run on, through a cmdlet that throws from the step it's told to.
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
}
