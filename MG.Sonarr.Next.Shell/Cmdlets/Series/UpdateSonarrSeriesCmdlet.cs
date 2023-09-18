using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsData.Update, "SonarrSeries", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [Alias("Set-SonarrSeries")]
    public sealed class UpdateSonarrSeriesCmdlet : SonarrApiCmdletBase
    {
        readonly List<PSObject> _list;

        public UpdateSonarrSeriesCmdlet()
            : base()
        {
            _list = new List<PSObject>(1);
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
            if (value is not null)
            {
                foreach (object item in value)
                {
                    if (item.IsCorrectType(Constants.SERIES, out var pso))
                    {
                        _list.Add(pso);
                    }
                }
            }
        }

        protected override void EndProcessing()
        {
            if (_list.Count <= 0)
            {
                this.WriteWarning("No series were passed via the pipeline. Make sure to pass the correct object type.");
                base.EndProcessing();
                return;
            }

            foreach (PSObject item in _list)
            {
                this.SerializeIfDebug(item, options: this.Options?.GetForSerializing());

                if (item.TryGetProperty(nameof(GetSonarrSeriesCmdlet.Id), out int id))
                {
                    string path = $"/series/{id}";
                    if (this.ShouldProcess(path, "Update Series"))
                    {
                        this.SendPutRequest(path, item);
                    }
                }
            }

            base.EndProcessing();
        }
    }
}
