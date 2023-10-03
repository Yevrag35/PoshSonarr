using MG.Sonarr.Next.Services.Models.Series;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsData.Update, "SonarrSeries", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [Alias("Set-SonarrSeries")]
    public sealed class UpdateSonarrSeriesCmdlet : SonarrApiCmdletBase
    {
        List<SeriesObject>? _list = null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public SeriesObject[] InputObject
        {
            get => Array.Empty<SeriesObject>();
            set
            {
                _list ??= new(value.Length);
                _list.AddRange(value);
            }
        }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
        }
        protected override void End(IServiceProvider provider)
        {
            if (_list is null || _list.Count <= 0)
            {
                this.WriteWarning("No series were passed via the pipeline. Make sure to pass the correct object type.");
                return;
            }

            foreach (SeriesObject item in _list)
            {
                this.SerializeIfDebug(item, options: this.Options?.GetForSerializing());

                string path = $"/series/{item.Id}";
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
