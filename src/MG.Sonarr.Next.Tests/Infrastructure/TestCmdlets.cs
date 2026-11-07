using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Cmdlets;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerShell.Commands;
using System.Collections.ObjectModel;
using System.Management.Automation.Runspaces;

namespace MG.Sonarr.Next.Tests.Infrastructure;

/// <summary>
/// Opens runspaces that hold the test cmdlets, and runs them.
/// </summary>
/// <remarks>
/// Every runspace in the process shares the module's one connection, as runspaces in a PowerShell session do, so a
/// test that connects runs in <see cref="ConnectionCollection"/>, which no other test runs beside.
/// </remarks>
internal static class PowerShellHost
{
	/// <summary>
	/// Creates a <see cref="PowerShell"/> instance on a new runspace that holds the test cmdlets, <c>Connect-FakeSonarr</c>,
	/// <c>Disconnect-FakeSonarr</c>, <c>Test-Preference</c>, <c>Test-Probe</c>, and <c>Test-Request</c>, and PowerShell's
	/// <c>Select-Object</c> and <c>Write-Output</c>.
	/// </summary>
	/// <remarks>
	/// PowerShell's two cmdlets come from their types, so the runspace holds them without loading the module that ships
	/// them.
	/// </remarks>
	/// <returns>The new instance, which the caller disposes.</returns>
	internal static PowerShell Create()
	{
		InitialSessionState state = InitialSessionState.CreateDefault2();
		state.Commands.Add(new SessionStateCmdletEntry("Connect-FakeSonarr", typeof(ConnectFakeSonarrCmdlet), helpFileName: null));
		state.Commands.Add(new SessionStateCmdletEntry("Disconnect-FakeSonarr", typeof(DisconnectFakeSonarrCmdlet), helpFileName: null));
		state.Commands.Add(new SessionStateCmdletEntry("Test-Preference", typeof(TestPreferenceCmdlet), helpFileName: null));
		state.Commands.Add(new SessionStateCmdletEntry("Test-Probe", typeof(TestProbeCmdlet), helpFileName: null));
		state.Commands.Add(new SessionStateCmdletEntry("Test-Request", typeof(TestRequestCmdlet), helpFileName: null));
		state.Commands.Add(new SessionStateCmdletEntry("Select-Object", typeof(SelectObjectCommand), helpFileName: null));
		state.Commands.Add(new SessionStateCmdletEntry("Write-Output", typeof(WriteOutputCommand), helpFileName: null));
		state.ThrowOnRunspaceOpenError = true;

		return PowerShell.Create(state);
	}

	/// <summary>
	/// Makes a connection to the specified fake instance the module's connection.
	/// </summary>
	/// <param name="powerShell">The instance to run <c>Connect-FakeSonarr</c> in.</param>
	/// <param name="sonarr">The fake instance to connect to.</param>
	internal static void Connect(this PowerShell powerShell, FakeSonarr sonarr)
	{
		_ = powerShell.Run("Connect-FakeSonarr", ("Server", sonarr));
		Assert.Empty(powerShell.Streams.Error);
	}

	/// <summary>
	/// Removes the module's connection.
	/// </summary>
	/// <param name="powerShell">The instance to run <c>Disconnect-FakeSonarr</c> in.</param>
	internal static void Disconnect(this PowerShell powerShell)
	{
		_ = powerShell.Run("Disconnect-FakeSonarr");
	}

	/// <summary>
	/// Runs one command, with the specified parameters, as a new pipeline of the instance.
	/// </summary>
	/// <param name="powerShell">The instance to run the command in, whose earlier commands and streams are cleared.</param>
	/// <param name="command">The command's name.</param>
	/// <param name="parameters">The command's parameters, by name.</param>
	/// <returns>The command's output.</returns>
	internal static Collection<PSObject> Run(this PowerShell powerShell, string command, params (string Name, object? Value)[] parameters)
	{
		powerShell.Prepare(command, parameters);
		return powerShell.Invoke();
	}

	/// <summary>
	/// Clears the instance's earlier commands and streams and adds one command with the specified parameters.
	/// </summary>
	/// <param name="powerShell">The instance to prepare.</param>
	/// <param name="command">The command's name.</param>
	/// <param name="parameters">The command's parameters, by name.</param>
	/// <returns><paramref name="powerShell"/>, for chaining.</returns>
	internal static PowerShell Prepare(this PowerShell powerShell, string command, params (string Name, object? Value)[] parameters)
	{
		powerShell.Commands.Clear();
		powerShell.Streams.ClearStreams();
		_ = powerShell.AddCommand(command);
		foreach ((string name, object? value) in parameters)
		{
			_ = powerShell.AddParameter(name, value);
		}

		return powerShell;
	}
}

/// <summary>
/// The collection of the tests that change the module's connection, which every runspace in the process shares, so
/// they run one at a time and beside no other test.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConnectionCollection
{
	/// <summary>
	/// The collection's name.
	/// </summary>
	public const string Name = "Connection";
}

/// <summary>
/// Connects the module to a <see cref="FakeSonarr"/>, through the same base and builder as <c>Connect-SonarrInstance</c>.
/// </summary>
[Cmdlet(VerbsCommunications.Connect, "FakeSonarr")]
public sealed class ConnectFakeSonarrCmdlet : ConnectCmdlet
{
	/// <summary>
	/// Gets or sets the instance to connect to.
	/// </summary>
	[Parameter(Mandatory = true)]
	public FakeSonarr Server { get; set; } = null!;

	/// <inheritdoc/>
	protected override IConnectionSettings GetConnectionSettings()
	{
		return this.Server.Settings;
	}

	/// <summary>
	/// Makes the connection and resolves its <see cref="ConnectionProbe"/>, so the probe logs the provider's disposal.
	/// </summary>
	protected override void ProcessRecord()
	{
		using IServiceScope scope = this.ConnectContext(this.Server.AddServices);
		_ = scope.ServiceProvider.GetRequiredService<ConnectionProbe>();
	}
}

/// <summary>
/// Removes the module's connection, through the same base as <c>Disconnect-SonarrInstance</c>.
/// </summary>
[Cmdlet(VerbsCommunications.Disconnect, "FakeSonarr")]
public sealed class DisconnectFakeSonarrCmdlet : DisconnectCmdlet
{
	/// <summary>
	/// Removes the connection.
	/// </summary>
	protected override void ProcessRecord()
	{
		this.DisconnectContext();
	}
}

/// <summary>
/// Names the step of <see cref="TestProbeCmdlet"/> that throws.
/// </summary>
public enum ProbeStep
{
	/// <summary>No step throws.</summary>
	None,
	/// <summary><c>Begin</c> throws.</summary>
	Begin,
	/// <summary><c>Process</c> throws for the input that <see cref="TestProbeCmdlet.ThrowFor"/> names.</summary>
	Process,
	/// <summary><c>End</c> throws.</summary>
	End,
}

/// <summary>
/// A cmdlet on <see cref="SonarrCmdletBase"/> that logs each step that runs and throws from the step that it's told to.
/// </summary>
/// <remarks>
/// <c>Begin</c> resolves the scope's <see cref="ScopeProbe"/>, so the log shows when the scope is disposed, and, with
/// <see cref="HoldInBegin"/>, waits at the gate first and then writes the name of the connection that its scope is on.
/// </remarks>
[Cmdlet(VerbsDiagnostic.Test, "Probe")]
public sealed class TestProbeCmdlet : SonarrCmdletBase
{
	/// <summary>
	/// Gets or sets the log that the cmdlet writes its steps to.
	/// </summary>
	[Parameter(Mandatory = true)]
	public ProbeLog Log { get; set; } = null!;

	/// <summary>
	/// Gets or sets the input object, which <c>Process</c> logs.
	/// </summary>
	[Parameter(ValueFromPipeline = true)]
	public int InputObject { get; set; }

	/// <summary>
	/// Gets or sets the step that throws.
	/// </summary>
	[Parameter]
	public ProbeStep ThrowIn { get; set; }

	/// <summary>
	/// Gets or sets the input that <c>Process</c> throws for, when <see cref="ThrowIn"/> is
	/// <see cref="ProbeStep.Process"/>.
	/// </summary>
	[Parameter]
	public int ThrowFor { get; set; }

	/// <summary>
	/// Gets or sets the gate that <c>Begin</c> waits at, or <see langword="null"/> for none.
	/// </summary>
	[Parameter]
	public ProbeGate? HoldInBegin { get; set; }

	/// <inheritdoc/>
	protected override void Begin(IServiceProvider provider)
	{
		this.HoldInBegin?.EnterAndWait();

		ConnectionProbe connection = provider.GetRequiredService<ConnectionProbe>();
		_ = provider.GetRequiredService<ScopeProbe>();
		this.Log.Add("Begin");

		if (this.ThrowIn == ProbeStep.Begin)
		{
			throw new InvalidOperationException("Begin failed");
		}

		if (this.HoldInBegin is not null)
		{
			this.WriteObject(connection.Name);
		}
	}

	/// <inheritdoc/>
	protected override void Process(IServiceProvider provider)
	{
		this.Log.Add(string.Create(CultureInfo.InvariantCulture, $"Process {this.InputObject}"));

		if (this.ThrowIn == ProbeStep.Process && this.InputObject == this.ThrowFor)
		{
			throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"Process failed for {this.InputObject}"));
		}
	}

	/// <inheritdoc/>
	protected override void End(IServiceProvider provider)
	{
		this.Log.Add("End");

		if (this.ThrowIn == ProbeStep.End)
		{
			throw new InvalidOperationException("End failed");
		}
	}
}

/// <summary>
/// A cmdlet on <see cref="TimedCmdlet"/>, which captures its error, verbose, and debug preferences, that writes the
/// preferences that it runs with.
/// </summary>
[Cmdlet(VerbsDiagnostic.Test, "Preference")]
public sealed class TestPreferenceCmdlet : TimedCmdlet
{
	/// <summary>
	/// Writes the cmdlet's error, verbose, and debug preferences, in that order, as one string with a space between
	/// each two.
	/// </summary>
	/// <param name="provider">The scope's services, which the cmdlet doesn't use.</param>
	protected override void Begin(IServiceProvider provider)
	{
		this.WriteObject(string.Join(' ', this.ErrorPreference, this.VerbosePreference, this.DebugPreference));
	}
}

/// <summary>
/// A cmdlet on <see cref="SonarrApiCmdletBase"/> that sends one request through its <c>Send*Request</c> helpers, logs how
/// the request ended, and then logs whether its scope still serves.
/// </summary>
/// <remarks>
/// <c>Stop</c> logs that it ran and releases <see cref="Gate"/>, where <c>Process</c> waits once its request has ended,
/// so <c>Process</c> touches its scope only after <c>StopProcessing</c> has done what it does before it calls
/// <c>Stop</c>.
/// </remarks>
[Cmdlet(VerbsDiagnostic.Test, "Request")]
public sealed class TestRequestCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets or sets the log that the cmdlet writes its steps to.
	/// </summary>
	[Parameter(Mandatory = true)]
	public ProbeLog Log { get; set; } = null!;

	/// <summary>
	/// Gets or sets the gate that <c>Process</c> waits at once its request has ended, and that <c>Stop</c> releases.
	/// </summary>
	[Parameter(Mandatory = true)]
	public ProbeGate Gate { get; set; } = null!;

	/// <summary>
	/// Sends a GET request for the tags, waits at the gate, logs how the request ended, and resolves the scope's
	/// <see cref="ScopeProbe"/>.
	/// </summary>
	/// <param name="provider">The scope's services.</param>
	protected override void Process(IServiceProvider provider)
	{
		SonarrClientResult<object> result = this.SendGetRequest<object>("/tag");
		this.Gate.EnterAndWait();

		this.Log.Add(result.IsError
			? string.Concat("Request ended: ", result.Error.CategoryInfo.Category.ToString())
			: "Request succeeded");

		try
		{
			_ = provider.GetRequiredService<ScopeProbe>();
			this.Log.Add("Scope open");
		}
		catch (ObjectDisposedException)
		{
			this.Log.Add("Scope disposed under Process");
		}
	}

	/// <summary>
	/// Logs that the cmdlet was stopped, and releases <see cref="Gate"/>.
	/// </summary>
	/// <param name="provider">The scope's services, which the method doesn't use.</param>
	protected override void Stop(IServiceProvider provider)
	{
		this.Log.Add("Stop");
		this.Gate.Release();
	}
}
