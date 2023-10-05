using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Tags;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Remove, "SonarrTag", SupportsShouldProcess = true, DefaultParameterSetName = "ByPipelineInput")]
    [Alias("Delete-SonarrTag")]
    public sealed class RemoveSonarrTagCmdlet : SonarrApiCmdletBase
    {
        MetadataTag Tag { get; set; } = null!;

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ById")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Id { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipelineInput",
            DontShow = true)]
        public TagObject InputObject
        {
            get => null!;
            set => this.Id = value?.Id ?? 0;
        }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Tag = provider.GetRequiredService<MetadataResolver>()[Meta.TAG];
        }
        protected override void Process(IServiceProvider provider)
        {
            if (this.InvokeCommand.HasErrors || this.Id <= 0)
            {
                return;
            }

            string path = this.Tag.GetUrlForId(this.Id);
            if (this.ShouldProcess(path, "Delete Tag"))
            {
                var response = this.SendDeleteRequest(path);
                if (response.IsError)
                {
                    this.WriteError(response.Error);
                }
            }
        }
    }
}
