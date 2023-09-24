using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Tags;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Rename, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
        DefaultParameterSetName = "ById")]
    public sealed class RenameSonarrTagCmdlet : SonarrApiCmdletBase
    {
        readonly Dictionary<string, object> _dict = new(2, StringComparer.InvariantCultureIgnoreCase);

        MetadataTag Tag { get; }

        public RenameSonarrTagCmdlet()
            : base()
        {
            this.Tag = this.Services.GetRequiredService<MetadataResolver>()[Meta.TAG];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ById")]
        public int Id
        {
            get => default;
            set => _dict[nameof(this.Id)] = value;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipelineInput", DontShow = true)]
        public TagObject InputObject
        {
            get => null!;
            set => _dict[nameof(this.Id)] = value.Id;
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
            if (_dict.Count == 2)
            {
                string url = this.Tag.GetUrlForId((int)_dict[nameof(this.Id)]);
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
