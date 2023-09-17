namespace MG.Sonarr.Next.Shell.Cmdlets.System
{
    [Cmdlet(VerbsCommon.Get, "SonarrStatus")]
    public sealed class GetSonarrStatus : SonarrApiCmdletBase
    {
        protected override void ProcessRecord()
        {
            var result = this.Client.SendGet<PSObject>("/system/status").GetAwaiter().GetResult();
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
