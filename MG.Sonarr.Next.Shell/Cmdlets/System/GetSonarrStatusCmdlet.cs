namespace MG.Sonarr.Next.Shell.Cmdlets.System
{
    [Cmdlet(VerbsCommon.Get, "SonarrStatus")]
    public sealed class GetSonarrStatusCmdlet : SonarrApiCmdletBase
    {
        protected override ErrorRecord? Process()
        {
            var result = this.SendGetRequest<PSObject>("/system/status");
            if (result.IsError)
            {
                return result.Error;
            }
            else
            {
                this.WriteObject(result.Data);
                return null;
            }
        }
    }
}
