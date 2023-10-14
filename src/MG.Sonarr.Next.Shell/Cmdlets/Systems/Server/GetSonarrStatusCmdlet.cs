namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Server
{
    [Cmdlet(VerbsCommon.Get, "SonarrStatus")]
    public sealed class GetSonarrStatusCmdlet : SonarrApiCmdletBase
    {
        protected override void Process(IServiceProvider provider)
        {
            var result = this.SendGetRequest<PSObject>("/system/status");
            if (result.IsError)
            {
                this.StopCmdlet(result.Error);
                return;
            }
            else
            {
                this.WriteObject(result.Data);
            }
        }
    }
}
