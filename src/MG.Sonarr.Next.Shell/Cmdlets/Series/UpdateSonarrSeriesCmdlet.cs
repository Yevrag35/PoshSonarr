using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsData.Update, "SonarrSeries", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [Alias("Set-SonarrSeries")]
    [MetadataCanPipe(Tag = Meta.SERIES)]
    public sealed class UpdateSonarrSeriesCmdlet : SonarrApiCmdletBase
    {
        List<SeriesObject> _list = null!;

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public SeriesObject[] InputObject { get; set; } = Array.Empty<SeriesObject>();

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _list = new(1);
        }
        protected override void Process(IServiceProvider provider)
        {
            foreach (SeriesObject series in this.InputObject)
            {
                if (series.Id <= 0)
                {
                    this.WriteWarning("A series with an invalid ID was passed. It will be ignored.");
                    continue;
                }

                _list.Add(series);
            }
        }
        protected override void End(IServiceProvider provider)
        {
            MetadataTag tag = provider.GetMetadataTag(Meta.SERIES);

            if (_list is null || _list.Count <= 0)
            {
                this.WriteWarning("No series were passed via the pipeline. Make sure to pass the correct object type.");
                return;
            }

            foreach (SeriesObject item in _list)
            {
                this.SerializeIfDebug(
                    value: item,
                    options: provider.GetService<ISonarrJsonOptions>()?.ForSerializing);

                string path = tag.GetUrlForId(item.Id);
                if (this.ShouldProcess(path, "Update Series"))
                {
                    var response = this.SendPutRequest(path, item);
                    if (response.IsError)
                    {
                        this.WriteError(response.Error);
                        item.Reset();
                    }
                    else
                    {
                        item.Commit();
                    }
                }
            }
        }
    }
}
