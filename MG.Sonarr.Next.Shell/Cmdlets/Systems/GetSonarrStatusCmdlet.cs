namespace MG.Sonarr.Next.Shell.Cmdlets.Systems
{
    [Cmdlet(VerbsCommon.Get, "SonarrStatus")]
    public sealed class GetSonarrStatusCmdlet : SonarrApiCmdletBase
    {
        protected override void Process()
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
