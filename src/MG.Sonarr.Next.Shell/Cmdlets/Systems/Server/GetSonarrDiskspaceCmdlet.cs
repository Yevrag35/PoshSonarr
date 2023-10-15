namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Server
{
    [Cmdlet(VerbsCommon.Get, "SonarrDiskspace")]
    public sealed class GetSonarrDiskspaceCmdlet : SonarrApiCmdletBase
    {


        protected override void Process(IServiceProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            var response = this.SendGetRequest<List<PSObject>>(Constants.DISKSPACE);
            _ = this.TryWriteObject(in response, true);
        }
    }
}
