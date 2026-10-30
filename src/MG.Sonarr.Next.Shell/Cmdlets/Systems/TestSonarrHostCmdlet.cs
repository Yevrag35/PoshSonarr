using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Services;
using MG.Sonarr.Next.Shell.Settings;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems;

[Cmdlet(VerbsDiagnostic.Test, "SonarrHost"), Alias("Test-Sonarr")]
public sealed class TestSonarrHostCmdlet : TestConnectionCmdlet
{
	[Parameter(Mandatory = true, Position = 0)]
	[Alias("SonarrUrl", "Uri")]
	[ValidateUrl(UriKind.Absolute)]
	public Uri Url { get; set; } = null!;

	[Parameter(Mandatory = true, Position = 1)]
	[Alias("Key"), ValidateNotNullOrEmpty]
	public ApiKey ApiKey { get; set; }

	[Parameter, Alias("NoApiPrefix")]
	public SwitchParameter NoApiInPath { get; set; }

	[Parameter]
	public SwitchParameter Quiet { get; set; }

	[Parameter]
	public SwitchParameter SkipCertificateCheck { get; set; }

	[Parameter]
	[PSDefaultValue(Value = "5 minutes")]
	[ValidateTimeout]
	public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5d);

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
