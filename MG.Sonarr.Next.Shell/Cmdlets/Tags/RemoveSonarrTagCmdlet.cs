using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
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

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipelineInput",
            DontShow = true)]
        public object InputObject
        {
            get => null!;
            set
            {
                if (value.IsCorrectType(Meta.TAG, out PSObject? pso)
                    &&
                    pso.TryGetNonNullProperty(Constants.ID, out int id))
                {
                    this.Id = id;
                    return;
                }

                this.Error = new ArgumentException("Object is not the correct metadata type. Was expecting #tag. Did you mean to use \"Clear-SonarrTag\"?", nameof(this.InputObject)).ToRecord(ErrorCategory.InvalidArgument, value);
            }
        }

        protected override ErrorRecord? Process()
        {
            string path = this.Tag.GetUrlForId(this.Id);
            if (this.ShouldProcess(path, "Delete Tag"))
            {
                var response = this.SendDeleteRequest(path);
                return response.Error;
            }

            return null;
        }
    }
}
