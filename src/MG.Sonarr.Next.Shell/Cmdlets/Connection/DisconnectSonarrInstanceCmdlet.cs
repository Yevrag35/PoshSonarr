namespace MG.Sonarr.Next.Shell.Cmdlets.Connection
{
    [Cmdlet(VerbsCommunications.Disconnect, "SonarrInstance")]
    [Alias("Disconnect-Sonarr")]
    public sealed class DisconnectSonarrInstanceCmdlet : DisconnectCmdlet
    {
        protected override void ProcessRecord()
        {
            this.DisconnectContext();
            this.WriteVerbose("Service Provider was unset.");
        }
    }
}
