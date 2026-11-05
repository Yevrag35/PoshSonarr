using MG.Sonarr.Next.Services.Auth;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Server;

[Cmdlet(VerbsLifecycle.Restart, "SonarrInstance", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
[Alias("Restart-Sonarr")]
public sealed class RestartSonarrInstanceCmdlet : SonarrApiCmdletBase
{
	[Parameter]
	public SwitchParameter Force { get; set; }

	protected override void Process(IServiceProvider provider)
	{
		var settings = provider.GetRequiredService<IConnectionSettings>();
		string host = settings.ServiceUri.GetComponents(UriComponents.Host, UriFormat.Unescaped);
		if (!this.ShouldProcessWithForce(host, "Restart Sonarr Application", this.Force))
		{
			return;
		}

		var response = this.SendPostRequest<PSObject>(Constants.Restart);
		_ = this.TryWriteObject(response);
	}
}
