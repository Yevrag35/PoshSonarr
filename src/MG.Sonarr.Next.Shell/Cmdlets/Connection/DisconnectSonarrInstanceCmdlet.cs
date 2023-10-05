namespace MG.Sonarr.Next.Shell.Cmdlets.Connection
{
    [Cmdlet(VerbsCommunications.Disconnect, "SonarrInstance")]
    [Alias("Disconnect-Sonarr")]
    public sealed class DisconnectSonarrInstanceCmdlet : Cmdlet
    {
        protected override void ProcessRecord()
        {
            this.UnsetContext();
            this.WriteVerbose("Service Provider was unset.");
        }
    }
}
