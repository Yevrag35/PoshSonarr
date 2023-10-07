using MG.Sonarr.Next.Models.Indexers;

namespace MG.Sonarr.Next.Shell.Cmdlets.Indexers
{
    [Cmdlet(VerbsData.Update, "SonarrIndexer", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [Alias("Set-SonarrIndexer")]
    public sealed class UpdateSonarrIndexerCmdlet : SonarrApiCmdletBase
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [ValidateNotNull]
        public IndexerObject InputObject { get; set; } = null!;

        protected override void Process(IServiceProvider provider)
        {
            this.SerializeIfDebug(this.InputObject, options: this.Options?.GetForSerializing());

            string path = this.InputObject.MetadataTag.GetUrlForId(this.InputObject.Id);
            if (this.ShouldProcess(path, "Update Indexer"))
            {
                var response = this.SendPutRequest(path, this.InputObject);
                if (response.IsError)
                {
                    this.WriteConditionalError(response.Error);
                    this.InputObject.Reset();
                }
                else
                {
                    this.InputObject.Commit();
                }
            }
        }
    }
}
