using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Tags;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Rename, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
        DefaultParameterSetName = "ById")]
    [MetadataCanPipe(Tag = Meta.TAG)]
    public sealed class RenameSonarrTagCmdlet : SonarrApiCmdletBase
    {
        Dictionary<string, object?> _dict = null!;
        MetadataTag Tag { get; set; } = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ById")]
        public int Id
        {
            get => default;
            set
            {
                _dict ??= new(2);
                _dict[nameof(this.Id)] = value;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipelineInput", DontShow = true)]
        public TagObject InputObject
        {
            get => null!;
            set
            {
                _dict ??= new(2);
                _dict[nameof(this.Id)] = value?.Id ?? 0;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByPipelineInput")]
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "ById")]
        [ValidateNotNullOrEmpty]
        public string NewName
        {
            get => string.Empty;
            set
            {
                _dict ??= new(2);
                _dict[Constants.LABEL] = value;
            }
        }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Tag = provider.GetRequiredService<IMetadataResolver>()[Meta.TAG];
        }
        protected override void Process(IServiceProvider provider)
        {
            if (_dict.Count == 2 && _dict.TryGetValueAs(nameof(this.Id), out int id) && id > 0)
            {
                string url = this.Tag.GetUrlForId(id);
                if (this.ShouldProcess(url, "Update tag label"))
                {
                    var result = this.SendPutRequest(url, _dict);
                    if (result.IsError)
                    {
                        this.WriteError(result.Error);
                    }
                }
            }
        }
    }
}
