namespace MG.Sonarr.Next.Shell.Cmdlets.System
{
    [Cmdlet(VerbsCommon.Get, "SonarrStatus")]
    public sealed class GetSonarrStatus : SonarrApiCmdletBase
    {
        protected override void ProcessRecord()
        {
            var result = this.SendGetRequest<PSObject>("/system/status");
            if (result.IsError)
            {
                this.WriteError(result.Error);
            }
            else
            {
                this.WriteObject(result.Data);
            }
        }
    }
}
