using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Rename, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
        DefaultParameterSetName = "ById")]
    public sealed class RenameSonarrTagCmdlet : SonarrApiCmdletBase
    {
        readonly Dictionary<string, object> _dict = new(2, StringComparer.InvariantCultureIgnoreCase);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ById")]
        public int Id
        {
            get => default;
            set => _dict[nameof(this.Id)] = value;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipelineInput", DontShow = true)]
        public object InputObject
        {
            get => null!;
            set
            {
                if (value is not null
                    &&
                    value.IsCorrectType(Meta.TAG, out PSObject? pso)
                    &&
                    pso.TryGetProperty(nameof(this.Id), out int id))
                {
                    _dict[nameof(this.Id)] = id;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByPipelineInput")]
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "ById")]
        [ValidateNotNullOrEmpty]
        public string NewName
        {
            get => string.Empty;
            set => _dict[Constants.LABEL] = value;
        }

        protected override ErrorRecord? Process()
        {
            if (_dict.Count == 2 && _dict.TryGetValue(nameof(this.Id), out object? oid))
            {
                string url = $"/tag/{oid}";
                if (this.ShouldProcess(url, "Update tag label"))
                {
                    var result = this.SendPutRequest(url, _dict);
                    return result.Error;
                }
            }

            return null;
        }
    }
}
