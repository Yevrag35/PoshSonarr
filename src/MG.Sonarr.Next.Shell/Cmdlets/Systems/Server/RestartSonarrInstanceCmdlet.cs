using MG.Sonarr.Next.Services.Auth;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Server;

/// <summary>
/// Represents the <c>Restart-SonarrInstance</c> cmdlet, which restarts the Sonarr application.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet sends a <c>POST</c> to <c>/system/restart</c> and writes Sonarr's answer to the pipeline. While Sonarr
/// restarts, requests to it fail.
/// </para>
/// <para>
/// The cmdlet's confirm impact is high, so it asks for confirmation before the restart, naming the instance's host.
/// <c>-Force</c> skips the prompt, unless <c>-Confirm</c> is given, and in What if mode the cmdlet shows its What if
/// line and sends nothing. When the request fails, the cmdlet writes its error, unless the error is ignorable.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Restart-Sonarr</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsLifecycle.Restart, "SonarrInstance", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
[Alias("Restart-Sonarr")]
public sealed class RestartSonarrInstanceCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet restarts Sonarr without asking for confirmation.
	/// </summary>
	/// <remarks>
	/// The switch skips the prompt unless <c>-Confirm</c> is given. In What if mode, the cmdlet doesn't restart Sonarr,
	/// whether the switch is present or not.
	/// </remarks>
	/// <value>The switch, which is present to skip the confirmation.</value>
	[Parameter]
	public SwitchParameter Force { get; set; }

	/// <summary>
	/// Asks Sonarr to restart, once the user confirms, and writes Sonarr's answer to the pipeline.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
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
