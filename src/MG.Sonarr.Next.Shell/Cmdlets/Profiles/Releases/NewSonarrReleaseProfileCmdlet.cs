using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Unions;
using System.Collections;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Releases
{
    [Cmdlet(VerbsCommon.New, "SonarrReleaseProfile", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    public sealed class NewSonarrReleaseProfileCmdlet : SonarrMetadataCmdlet
    {
        protected override bool CaptureDebugPreference => true;
        public override bool CanDebugSerializeAfter => true;
        public override bool CanDebugSerializeBefore => true;

        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrWhiteSpace]
        [ValidateLength(1, int.MaxValue)]
        public string Name { get; set; } = string.Empty;

        [Parameter]
        public SwitchParameter Enabled { get; set; }

        [Parameter]
        [ValidateRange(ValidateRangeKind.Positive)]
        public Either<string, int> Indexer { get; set; } = default;

        [Parameter]
        [ValidateNotNull]
        [ValidateLength(1, int.MaxValue)]
        [Alias("Ignored")]
        public string[] IgnoredTerms { get; set; } = [];

        [Parameter]
        [ValidateNotNull]
        [ValidateLength(1, int.MaxValue)]
        [Alias("Required")]
        public string[] RequiredTerms { get; set; } = [];

        [Parameter]
        [ValidateNotNull]
        public Either<string, int>[] Tags { get; set; } = [];


        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.RELEASE_PROFILE];
        }

        protected override void Begin(IServiceProvider provider)
        {
            
        }

        [SuppressMessage("Style", "IDE0037:Use inferred member name", Justification = "Naming should not be tied to Cmdlet parameter names.")]
        protected override void Process(IServiceProvider provider)
        {
            var body = new
            {
                Name = this.Name,
                Enabled = this.Enabled.ToBool(),
                IndexerId = this.Indexer,
                Tags = this.Tags,
                Required = this.RequiredTerms,
                Ignored = this.IgnoredTerms,
            };

            this.SerializeIfDebug(body, includeType: false);

            if (this.ShouldProcess(this.Tag.UrlBase, $"Create New Release Profile -> '{this.Name}'"))
            {
                this.CreateProfile(body);
            }
        }

        private void CreateProfile<T>(T body) where T : notnull
        {
            Either<ReleaseProfileObject, SonarrErrorRecord> response = this.SendPostRequest<T, ReleaseProfileObject>(this.Tag.UrlBase, body);

            response.Match(
                success => this.WriteObject(success),
                fail => this.WriteError(fail));
        }
    }
}

