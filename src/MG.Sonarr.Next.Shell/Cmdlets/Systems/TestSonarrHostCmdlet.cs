using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Services;
using MG.Sonarr.Next.Shell.Settings;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems;

/// <summary>
/// Represents the <c>Test-SonarrHost</c> cmdlet, which tests whether a URL and an API key reach a Sonarr instance's
/// API.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet runs the same test that <c>Connect-SonarrInstance</c> runs, getting the server's status from
/// <c>/system/status</c> with the key, but on services of its own, which it disposes afterward, so it leaves the
/// module's connection as it was and works without one.
/// </para>
/// <para>
/// When the test succeeds, the cmdlet writes the server's status as a <see cref="SystemStatusObject"/>. When it fails,
/// as it does for a wrong key, a URL that doesn't answer, or one that doesn't point at Sonarr's API, the cmdlet writes
/// the test's error, so <c>-ErrorAction</c> decides what happens to it. With <c>-Quiet</c>, the cmdlet writes
/// <see langword="true"/> or <see langword="false"/> instead, and a failed test's message goes to the verbose stream.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Test-Sonarr</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsDiagnostic.Test, "SonarrHost"), Alias("Test-Sonarr")]
public sealed class TestSonarrHostCmdlet : TestConnectionCmdlet
{
	/// <summary>
	/// Gets or sets the URL of the Sonarr instance to test.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The URL has to be absolute. Its path, such as <c>/sonarr</c> in <c>https://host/sonarr</c>, is the instance's URL
	/// base, which goes in front of the test's path.
	/// </para>
	/// <para>
	/// The parameter's aliases are <c>-SonarrUrl</c> and <c>-Uri</c>.
	/// </para>
	/// </remarks>
	/// <value>The instance's absolute URL.</value>
	[Parameter(Mandatory = true, Position = 0)]
	[Alias("SonarrUrl", "Uri")]
	[ValidateUrl(UriKind.Absolute)]
	public Uri Url { get; set; } = null!;

	/// <summary>
	/// Gets or sets the API key that the test sends.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The key binds from a string or a <see cref="System.Security.SecureString"/>.
	/// </para>
	/// <para>
	/// The parameter's alias is <c>-Key</c>.
	/// </para>
	/// </remarks>
	/// <value>The instance's API key.</value>
	[Parameter(Mandatory = true, Position = 1)]
	[Alias("Key"), ValidateNotNullOrEmpty]
	public ApiKey ApiKey { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether the test's request leaves <c>/api</c> out of its path.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The switch is for a reverse proxy that removes <c>/api</c> from the path, so the request goes to <c>/v3/</c> under
	/// the URL base instead of <c>/api/v3/</c>.
	/// </para>
	/// <para>
	/// The parameter's alias is <c>-NoApiPrefix</c>.
	/// </para>
	/// </remarks>
	/// <value>The switch, which is present to send the request without the <c>/api</c> prefix.</value>
	[Parameter, Alias("NoApiPrefix")]
	public SwitchParameter NoApiInPath { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet writes only whether the test succeeded.
	/// </summary>
	/// <remarks>
	/// A failed test writes no error then, and its message goes to the verbose stream.
	/// </remarks>
	/// <value>
	/// The switch, which is present to write <see langword="true"/> or <see langword="false"/> in place of the status or
	/// the error.
	/// </value>
	[Parameter]
	public SwitchParameter Quiet { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether the test accepts any server certificate.
	/// </summary>
	/// <remarks>
	/// The switch is for an instance with a self-signed or otherwise untrusted certificate.
	/// </remarks>
	/// <value>The switch, which is present to skip validating the server's certificate.</value>
	[Parameter]
	public SwitchParameter SkipCertificateCheck { get; set; }

	/// <summary>
	/// Gets or sets how long the test's request waits for a response before it fails.
	/// </summary>
	/// <remarks>
	/// The timeout has to be greater than zero and at most <see cref="int.MaxValue"/> milliseconds, about 24.8 days, so
	/// an infinite timeout is turned away too. PowerShell reads a number as ticks and a string such as <c>'30'</c> as
	/// days, so give a <see cref="TimeSpan"/>, such as <c>(New-TimeSpan -Seconds 30)</c> or <c>'00:00:30'</c>.
	/// </remarks>
	/// <value>The time that the request waits. The default is five minutes.</value>
	[Parameter]
	[PSDefaultValue(Value = "5 minutes")]
	[ValidateTimeout]
	public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5d);

	/// <summary>
	/// Builds services for the cmdlet's settings, tests the URL and API key with them, and writes the outcome, then
	/// disposes the services.
	/// </summary>
	/// <remarks>
	/// The services' scope holds no cmdlet, so the test's request writes no verbose or debug lines.
	/// </remarks>
	protected override void EndProcessing()
	{
		ConnectionSettings settings = new()
		{
			Key = this.ApiKey,
			NoApiInPath = this.NoApiInPath,
			ServiceUri = this.Url,
			SkipCertValidation = this.SkipCertificateCheck,
			Timeout = this.Timeout,
		};

		// The test is Connect-SonarrInstance's, so it fails as a connection would, and it runs on services of its own.
		using ServiceProvider provider = this.CreateTestContext(settings, ModuleServiceConfigurer.AddConfiguration);
		using IServiceScope scope = provider.CreateScope();

		var result = scope.ServiceProvider.GetRequiredService<ISonarrClient>().SendTest<SystemStatusObject>();
		if (this.Quiet)
		{
			if (result.IsError)
			{
				this.WriteVerbose(result.Error.Exception.Message);
			}

			this.WriteObject(!result.IsError);
		}
		else if (result.IsError)
		{
			// A failed test is one that a caller can skip, so -ErrorAction decides what happens to its error.
			this.WriteError(result.Error);
		}
		else
		{
			this.WriteObject(result.Value);
		}
	}
}
