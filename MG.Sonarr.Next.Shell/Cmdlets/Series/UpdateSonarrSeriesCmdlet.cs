using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Models;
using MG.Sonarr.Next.Services.Models.Series;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsData.Update, "SonarrSeries", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [Alias("Set-SonarrSeries")]
    public sealed class UpdateSonarrSeriesCmdlet : SonarrApiCmdletBase
    {
        readonly List<SeriesObject> _list;

        public UpdateSonarrSeriesCmdlet()
            : base()
        {
            _list = new List<SeriesObject>(1);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public object[] InputObject
        {
            get => Array.Empty<object>();
            set => this.AddArrayToList(value);
        }

        private void AddArrayToList(object[] value)
        {
            foreach (SeriesObject item in value?.OfType<SeriesObject>() ?? Enumerable.Empty<SeriesObject>())
            {
                _list.Add(item);
            }
        }

        protected override ErrorRecord? End()
        {
            if (_list.Count <= 0)
            {
                this.WriteWarning("No series were passed via the pipeline. Make sure to pass the correct object type.");
                return null;
            }

            foreach (SeriesObject item in _list)
            {
                this.SerializeIfDebug(item, options: this.Options?.GetForSerializing());

                string path = $"/series/{item.Id}";
                if (this.ShouldProcess(path, "Update Series"))
                {
                    this.SendPutRequest(path, item);
                }
            }

            return null;
        }
    }
}
