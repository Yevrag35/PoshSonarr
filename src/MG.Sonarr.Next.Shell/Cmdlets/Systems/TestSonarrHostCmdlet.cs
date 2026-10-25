using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Settings;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems;

[Cmdlet(VerbsDiagnostic.Test, "SonarrHost"), Alias("Test-Sonarr")]
public sealed class TestSonarrHostCmdlet : PSCmdlet
{
	private static readonly ScriptBlock _testBlock = ScriptBlock.Create("param ($url, $key) Invoke-RestMethod -Uri \"$($url.ToString())ping\" -Headers @{ 'X-Api-Key' = $key } -Method Get -ContentType 'application/json'");

	[Parameter(Mandatory = true, Position = 0)]
	[Alias("SonarrUrl", "Uri")]
	[ValidateUrl(UriKind.Absolute), MaybeNull]
	public Uri Url { get; set; } = null!;

	[Parameter(Mandatory = true, Position = 1)]
	[Alias("Key"), ValidateNotNullOrEmpty]
	public ApiKey ApiKey { get; set; }

	[Parameter]
	public SwitchParameter Quiet { get; set; }

	protected override void EndProcessing()
	{
		object response = _testBlock.InvokeReturnAsIs(this.Url, this.ApiKey.GetValue());
		if (!this.Quiet)
		{
			this.WriteObject(response, enumerateCollection: true);
		}
		else
		{
			this.WriteObject(response is PSObject pso && "OK".Equals(pso.Properties["status"]?.Value as string, StringComparison.OrdinalIgnoreCase));
		}
	}
}

