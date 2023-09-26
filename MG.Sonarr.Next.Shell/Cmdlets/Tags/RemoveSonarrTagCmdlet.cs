using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Tags;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Remove, "SonarrTag", SupportsShouldProcess = true, DefaultParameterSetName = "ByPipelineInput")]
    public sealed class RemoveSonarrTagCmdlet : SonarrApiCmdletBase
    {
        MetadataTag Tag { get; }

        public RemoveSonarrTagCmdlet()
            : base()
        {
            this.Tag = this.Services.GetRequiredService<MetadataResolver>()[Meta.TAG];
        }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ById")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Id { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipelineInput",
            DontShow = true)]
        public TagObject InputObject
        {
            get => null!;
            set => this.Id = value.Id;
        }

        protected override void Process()
        {
            if (this.InvokeCommand.HasErrors)
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
