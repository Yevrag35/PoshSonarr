namespace MG.Sonarr.Next.Shell.Cmdlets.Connection;

/// <summary>
/// Represents the <c>Disconnect-SonarrInstance</c> cmdlet, which removes the module's connection to a Sonarr instance.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet sends nothing to Sonarr and writes nothing to the pipeline. Removing the connection affects every
/// runspace in the process, so cmdlets that start afterward fail until <c>Connect-SonarrInstance</c> makes a new one,
/// while a cmdlet that is running on the connection finishes on it. When there's no connection, the cmdlet does
/// nothing.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Disconnect-Sonarr</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommunications.Disconnect, "SonarrInstance")]
[Alias("Disconnect-Sonarr")]
public sealed class DisconnectSonarrInstanceCmdlet : DisconnectCmdlet
{
	/// <summary>
	/// Removes the module's connection and writes a verbose message that says so.
	/// </summary>
	/// <remarks>
	/// The message is written whether or not there was a connection to remove.
	/// </remarks>
	protected override void ProcessRecord()
	{
		this.DisconnectContext();
		this.WriteVerbose("Service Provider was unset.");
	}
}
