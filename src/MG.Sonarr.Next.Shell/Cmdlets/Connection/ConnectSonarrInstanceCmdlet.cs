using MG.Sonarr.Next.Exceptions.Shell;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Services;
using MG.Sonarr.Next.Shell.Settings;
using MG.Sonarr.Resources;
using System.Management.Automation.Host;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets.Connection;

/// <summary>
/// Represents the <c>Connect-SonarrInstance</c> cmdlet, which connects the module to a Sonarr instance.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet builds the module's connection from <c>-Url</c>, <c>-ApiKey</c>, and its other parameters, and tests it
/// by getting the server's status from <c>/system/status</c> with the key. The new connection replaces the earlier one,
/// if any, once it's built, and every runspace in the process shares it. A cmdlet that is running on the earlier
/// connection finishes on it.
/// </para>
/// <para>
/// When the test fails, as it does for a wrong key, a URL that doesn't answer, or one that doesn't point at Sonarr's
/// API, the cmdlet removes the connection again, so the module has none, and ends the statement with the test's error,
/// which <c>-ErrorAction</c> can't hide.
/// </para>
/// <para>
/// The cmdlet writes nothing to the pipeline, unless <c>-PassThru</c> is present, which writes the server's status as
/// a <see cref="SystemStatusObject"/>. The verbose and debug lines for the test's request go to the host's UI, so
/// redirecting the verbose or debug stream doesn't capture them.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Connect-Sonarr</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommunications.Connect, "SonarrInstance", DefaultParameterSetName = "None"), Alias("Connect-Sonarr")]
public sealed class ConnectSonarrInstanceCmdlet : ConnectCmdlet, IApiCmdlet
{
	private ConnectionSettings _settings = null!;

	private ActionPreference _debugPreference;
	private ActionPreference _verbosePreference;

	/// <summary>
	/// Gets a value that indicates whether the response's JSON body is written to the debug stream after the response is
	/// received.
	/// </summary>
	/// <remarks>
	/// The cmdlet reads its debug preference from <c>-Debug</c>, or else from <c>$DebugPreference</c>, when it begins.
	/// </remarks>
	/// <value>
	/// <see langword="true"/> if the cmdlet's debug preference is anything other than
	/// <see cref="ActionPreference.SilentlyContinue"/>; otherwise, <see langword="false"/>.
	/// </value>
	public bool CanDebugSerializeAfter => _debugPreference != ActionPreference.SilentlyContinue;
	/// <summary>
	/// Gets a value that indicates whether the request's JSON body is written to the debug stream before the request is
	/// sent.
	/// </summary>
	/// <remarks>
	/// The cmdlet reads its debug preference from <c>-Debug</c>, or else from <c>$DebugPreference</c>, when it begins.
	/// </remarks>
	/// <value>
	/// <see langword="true"/> if the cmdlet's debug preference is anything other than
	/// <see cref="ActionPreference.SilentlyContinue"/>; otherwise, <see langword="false"/>.
	/// </value>
	public bool CanDebugSerializeBefore => _debugPreference != ActionPreference.SilentlyContinue;

	/// <summary>
	/// Gets or sets the API key that the module sends with every request to the instance.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The key binds from a string or a <see cref="System.Security.SecureString"/>. A key that is empty or only white
	/// space ends the cmdlet with a parameter error before it connects.
	/// </para>
	/// <para>
	/// The parameter's alias is <c>-Key</c>.
	/// </para>
	/// </remarks>
	/// <value>The instance's API key, or an empty key until one is set.</value>
	[Parameter(Mandatory = true, Position = 1)]
	[Alias("Key"), ValidateNotNullOrEmpty]
	public ApiKey ApiKey
	{
		get => _settings?.Key ?? ApiKey.Empty;
		set => this.SetConnectionSetting(value, (x, settings) => settings.Key = x);
	}

	/// <summary>
	/// Gets or sets the URL of the Sonarr instance.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The URL has to be absolute. Its path, such as <c>/sonarr</c> in <c>https://host/sonarr</c>, is the instance's URL
	/// base, which goes in front of the path of every request.
	/// </para>
	/// <para>
	/// The parameter's aliases are <c>-SonarrUrl</c> and <c>-Uri</c>.
	/// </para>
	/// </remarks>
	/// <value>The instance's absolute URL, or <see langword="null"/> until one is set.</value>
	[Parameter(Mandatory = true, Position = 0)]
	[Alias("SonarrUrl", "Uri")] // for backward compatibility
	[ValidateUrl(UriKind.Absolute), MaybeNull]
	public Uri Url
	{
		get => _settings?.ServiceUri;
		set => this.SetConnectionSetting(value, (x, settings) => settings.ServiceUri = x);
	}

	/// <summary>
	/// Gets or sets a value that indicates whether requests leave <c>/api</c> out of their paths.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The switch is for a reverse proxy that removes <c>/api</c> from the path, so requests go to <c>/v3/</c> under the
	/// URL base instead of <c>/api/v3/</c>.
	/// </para>
	/// <para>
	/// The parameter's alias is <c>-NoApiPrefix</c>.
	/// </para>
	/// </remarks>
	/// <value>The switch, which is present to send requests without the <c>/api</c> prefix.</value>
	[Parameter, Alias("NoApiPrefix")]  // for backward-compatibility
	public SwitchParameter NoApiInPath
	{
		get => _settings?.NoApiInPath ?? default;
		set => this.SetConnectionSetting(value.ToBool(), (x, settings) => settings.NoApiInPath = x);
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet writes the server's status to the pipeline.
	/// </summary>
	/// <remarks>
	/// The status comes from the connection's test, so the cmdlet sends no other request for it.
	/// </remarks>
	/// <value>The switch, which is present to write the status.</value>
	[Parameter]
	public SwitchParameter PassThru { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether the connection accepts any server certificate.
	/// </summary>
	/// <remarks>
	/// The switch is for an instance with a self-signed or otherwise untrusted certificate. Without the check, the
	/// connection can't tell the instance from a server that impersonates it.
	/// </remarks>
	/// <value>The switch, which is present to skip validating the server's certificate.</value>
	[Parameter]
	public SwitchParameter SkipCertificateCheck
	{
		get => _settings?.SkipCertValidation ?? default;
		set => this.SetConnectionSetting(value.ToBool(), (x, settings) => settings.SkipCertValidation = x);
	}

	/// <summary>
	/// Gets or sets how long each request of the connection waits for a response before it fails.
	/// </summary>
	/// <remarks>
	/// The timeout has to be greater than zero and at most <see cref="int.MaxValue"/> milliseconds, about 24.8 days, so
	/// an infinite timeout is turned away too. PowerShell reads a number as ticks and a string such as <c>'30'</c> as
	/// days, so give a <see cref="TimeSpan"/>, such as <c>(New-TimeSpan -Seconds 30)</c> or <c>'00:00:30'</c>.
	/// </remarks>
	/// <value>The time that a request waits. The default is five minutes.</value>
	[Parameter]
	[PSDefaultValue(Value = "5 minutes")]
	[ValidateTimeout]
	public TimeSpan Timeout
	{
		get => _settings?.Timeout ?? TimeSpan.Zero;
		set => this.SetConnectionSetting(value, (x, settings) => settings.Timeout = x);
	}

	/// <summary>
	/// Gets a value that indicates whether the cmdlet writes verbose messages about its requests.
	/// </summary>
	/// <remarks>
	/// The cmdlet reads its verbose preference from <c>-Verbose</c>, or else from <c>$VerbosePreference</c>, when it
	/// begins.
	/// </remarks>
	/// <value>
	/// <see langword="true"/> if the cmdlet's verbose preference is neither
	/// <see cref="ActionPreference.SilentlyContinue"/> nor <see cref="ActionPreference.Ignore"/>; otherwise,
	/// <see langword="false"/>.
	/// </value>
	public bool CanWriteVerbose => _verbosePreference is not (ActionPreference.SilentlyContinue or ActionPreference.Ignore);

	/// <summary>
	/// Creates the connection's settings when no parameter has created them, and stores the cmdlet's verbose and debug
	/// preferences.
	/// </summary>
	/// <remarks>
	/// The preferences come from <c>-Verbose</c> and <c>-Debug</c>, or else from <c>$VerbosePreference</c> and
	/// <c>$DebugPreference</c>, and decide what the cmdlet writes for the test's request.
	/// </remarks>
	protected override void BeginProcessing()
	{
		_settings ??= new();
		this.StorePreferences();
	}
	/// <summary>
	/// Checks the connection's settings, builds the module's connection with them, and tests it, and when the test fails,
	/// removes the connection again and ends the statement with the test's error.
	/// </summary>
	/// <remarks>
	/// A settings error, such as an empty API key, ends the statement before the cmdlet connects. The scope that the test
	/// runs in is disposed when the method returns, and a connection that passed its test stays the module's.
	/// </remarks>
	protected override void ProcessRecord()
	{
		this.ValidateSettings(_settings);

		using IServiceScope scope = this.ConnectContext(ModuleServiceConfigurer.AddConfiguration);

		var client = scope.ServiceProvider.GetRequiredService<ISonarrClient>();

		ISonarrResponse result = this.SendTest(client, scope.ServiceProvider, this.PassThru);

		if (result.IsError)
		{
			this.DisconnectContext(scope);
			this.ThrowTerminatingError(result.Error);
		}
	}

	/// <summary>
	/// Sends the connection's test with the specified client, and writes the server's status to the pipeline when the
	/// specified flag asks for it.
	/// </summary>
	/// <remarks>
	/// Both kinds of test store the server's authentication type in the connection's settings when they succeed:
	/// <see cref="ISonarrClient.SendTest(CancellationToken)"/> itself, and with <paramref name="passThru"/>, this method, from the status
	/// that it writes. The status comes from the same test, so a wrong key or a URL that doesn't reach Sonarr's API fails
	/// in the same way either way.
	/// </remarks>
	/// <param name="client">The client of the new connection's scope.</param>
	/// <param name="provider">The service provider of the new connection's scope.</param>
	/// <param name="passThru">
	/// <see langword="true"/> to write the server's status; otherwise, <see langword="false"/>.
	/// </param>
	/// <returns>The outcome of the test.</returns>
	private ISonarrResponse SendTest(ISonarrClient client, IServiceProvider provider, bool passThru)
	{
		if (!passThru)
		{
			return client.SendTest();
		}

		// The status comes from the same test, so a wrong key or a URL that doesn't reach Sonarr's API fails as it does
		// without -PassThru.
		var response = client.SendTest<SystemStatusObject>();
		if (!response.IsError)
		{
			var settings = provider.GetRequiredService<IConnectionSettings>();
			settings.AuthType = response.Value.Authentication;

			this.WriteObject(response.Value);
		}

		return response;
	}

	/// <summary>
	/// Checks that the specified settings hold an API key and a URL, and ends the statement with a parameter error when
	/// either one is missing or malformed.
	/// </summary>
	/// <remarks>
	/// <para>
	/// An empty key is reported as missing, and a key that is only white space as malformed, both against
	/// <c>-ApiKey</c>. A missing URL is reported as malformed, against <c>-Url</c>. Each error is a
	/// <see cref="SonarrParameterException"/> that wraps the exception that <see cref="ConnectionSettings.Validate"/>
	/// threw, and the method raises it with <see cref="Cmdlet.ThrowTerminatingError(ErrorRecord)"/>.
	/// </para>
	/// <para>
	/// A timeout that isn't greater than zero would make <see cref="ConnectionSettings.Validate"/> throw too, but
	/// <c>[ValidateTimeout]</c> on <c>-Timeout</c> turns such a value away when the arguments bind.
	/// </para>
	/// </remarks>
	/// <param name="settings">The settings that the cmdlet's parameters filled.</param>
	/// <exception cref="PipelineStoppedException">
	/// Thrown when the settings hold no usable API key or no URL, after PowerShell has recorded the parameter error.
	/// </exception>
	private void ValidateSettings(ConnectionSettings settings)
	{
		ParameterErrorType type = ParameterErrorType.Invalid;

		try
		{
			settings.Validate();
		}
		catch (InvalidApiKeyException keyEx)
		{
			string? key = settings.Key.GetValue();
			if (string.IsNullOrEmpty(key))
			{
				type = ParameterErrorType.Missing;
			}
			else
			{
				type |= ParameterErrorType.Malformed;
			}

			SonarrParameterException pEx = new(nameof(this.ApiKey), type, null, keyEx);
			this.ThrowTerminatingError(pEx.ToRecord());
		}
		catch (ArgumentNullException nullEx)
		{
			type |= ParameterErrorType.Malformed;
			SonarrParameterException pEx = new(nameof(this.Url), type, null, nullEx);
			this.ThrowTerminatingError(pEx.ToRecord(settings.ServiceUri));
		}
	}

	/// <summary>
	/// Releases the cmdlet's reference to the connection's settings.
	/// </summary>
	/// <remarks>
	/// The module's connection keeps its own reference to the settings, so the connection is unaffected.
	/// </remarks>
	protected override void EndProcessing()
	{
		_settings = null!;
	}

	/// <summary>
	/// Gets the settings that the cmdlet's parameters filled, which the connection is built with.
	/// </summary>
	/// <returns>The connection's settings.</returns>
	protected override IConnectionSettings GetConnectionSettings()
	{
		return _settings;
	}

	/// <summary>
	/// Determines whether the cmdlet writes verbose messages and its host has a UI to write them to.
	/// </summary>
	/// <param name="hostInterface">
	/// When the method returns, the host's UI, or <see langword="null"/> when the host has none.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <see cref="CanWriteVerbose"/> is <see langword="true"/> and the host has a UI;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private bool IsVerboseNotSilentAndUICanWrite([NotNullWhen(true)] out PSHostUserInterface? hostInterface)
	{
		hostInterface = this.Host?.UI;
		return this.CanWriteVerbose && hostInterface is not null;
	}
	/// <summary>
	/// Determines whether the cmdlet writes debug messages and its host has a UI to write them to.
	/// </summary>
	/// <param name="hostInterface">
	/// When the method returns, the host's UI, or <see langword="null"/> when the host has none.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the cmdlet's debug preference is neither <see cref="ActionPreference.SilentlyContinue"/>
	/// nor <see cref="ActionPreference.Ignore"/> and the host has a UI; otherwise, <see langword="false"/>.
	/// </returns>
	private bool IsDebugNotSilentAndUICanWrite([NotNullWhen(true)] out PSHostUserInterface? hostInterface)
	{
		hostInterface = this.Host?.UI;
		return _debugPreference is not (ActionPreference.SilentlyContinue or ActionPreference.Ignore) && hostInterface is not null;
	}

	/// <summary>
	/// Sets one of the connection's settings to the specified value, creating the settings first when no parameter has.
	/// </summary>
	/// <remarks>
	/// The parameters' setters run while the arguments bind, before <see cref="BeginProcessing"/>, so the first one to
	/// run creates the settings. A <see langword="null"/> value leaves the settings as they are.
	/// </remarks>
	/// <typeparam name="T">The type of the setting.</typeparam>
	/// <param name="value">The setting's new value, or <see langword="null"/> to leave it as it is.</param>
	/// <param name="setValue">A callback that stores the value in the settings.</param>
	private void SetConnectionSetting<T>(T? value, Action<T, ConnectionSettings> setValue)
	{
		if (value is not null)
		{
			_settings ??= new();
			setValue.Invoke(value, _settings);
		}
	}

	/// <summary>
	/// Stores the cmdlet's verbose and debug preferences, from <c>-Verbose</c> and <c>-Debug</c>, or else from
	/// <c>$VerbosePreference</c> and <c>$DebugPreference</c>.
	/// </summary>
	private void StorePreferences()
	{
		_verbosePreference = this.GetActionPreferenceFromSwitch(PSConstants.Verbose, PSConstants.VerbosePreference);
		_debugPreference = this.GetActionPreferenceFromSwitch(PSConstants.Debug, PSConstants.DebugPreference);
	}
	/// <summary>
	/// Writes the body of a request or a response to the debug stream.
	/// </summary>
	/// <remarks>
	/// The text goes to the host's UI as a debug line, so it can be written from any thread, and redirecting the debug
	/// stream doesn't capture it. Nothing is written when the cmdlet's debug preference is
	/// <see cref="ActionPreference.SilentlyContinue"/> or <see cref="ActionPreference.Ignore"/>, or when the host has no
	/// UI.
	/// </remarks>
	/// <param name="jsonPayload">The text to write, which describes the body and holds its JSON.</param>
	public void WriteDebugPayload(string jsonPayload)
	{
		if (this.IsDebugNotSilentAndUICanWrite(out PSHostUserInterface? hostInterface))
		{
			hostInterface.WriteDebugLine(jsonPayload);
		}
	}
	/// <summary>
	/// Writes a verbose line about a request that is about to be sent, with its method and URL.
	/// </summary>
	/// <remarks>
	/// The line goes to the host's UI, so it can be written from any thread, and redirecting the verbose stream doesn't
	/// capture it. Nothing is written when <see cref="CanWriteVerbose"/> is <see langword="false"/> or the host has no
	/// UI.
	/// </remarks>
	/// <param name="request">The details of the pending request.</param>
	public void WriteVerboseBefore(IHttpRequestDetails request)
	{
		if (this.IsVerboseNotSilentAndUICanWrite(out PSHostUserInterface? hostUI))
		{
			string msg = string.Format(
				provider: CultureInfo.CurrentCulture,
				format: Messages.Verbose_SendingRequest_Format,
				request.RequestMethod, request.RequestUrl);

			hostUI.WriteVerboseLine(msg);
		}
	}
	/// <summary>
	/// Writes a verbose line that reports the response to a request, as the JSON of the request's outcome.
	/// </summary>
	/// <remarks>
	/// The line goes to the host's UI, so it can be written from any thread, and redirecting the verbose stream doesn't
	/// capture it. Nothing is written when <see cref="CanWriteVerbose"/> is <see langword="false"/> or the host has no
	/// UI.
	/// </remarks>
	/// <param name="response">The outcome of the request, with the response's status code and URL.</param>
	/// <param name="provider">
	/// A scoped service provider, whose <see cref="ISonarrJsonOptions"/> serializes the outcome when
	/// <paramref name="options"/> is <see langword="null"/>.
	/// </param>
	/// <param name="options">
	/// The JSON options to serialize the outcome with, or <see langword="null"/> to use the serializing options of
	/// <paramref name="provider"/>.
	/// </param>
	public void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options = null)
	{
		if (this.IsVerboseNotSilentAndUICanWrite(out PSHostUserInterface? hostUI))
		{
			options ??= provider.GetService<ISonarrJsonOptions>()?.ForSerializing;
			string json = JsonSerializer.Serialize(response, options);
			hostUI.WriteVerboseLine(json);
		}
	}
}
