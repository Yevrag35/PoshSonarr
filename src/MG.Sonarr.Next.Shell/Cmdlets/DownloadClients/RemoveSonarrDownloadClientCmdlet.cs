namespace MG.Sonarr.Next.Shell.Cmdlets.DownloadClients
{
    [Cmdlet(VerbsCommon.Remove, "SonarrDownloadClient", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true,
        DefaultParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
    public sealed class RemoveSonarrDownloadClientCmdlet : SonarrApiCmdletBase
    {

    }
}

