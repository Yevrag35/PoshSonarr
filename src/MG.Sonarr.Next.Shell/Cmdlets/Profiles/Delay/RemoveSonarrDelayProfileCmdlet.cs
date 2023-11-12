using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Delay
{
    [Cmdlet(VerbsCommon.Remove, "SonarrDelayProfile", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [MetadataCanPipe(Tag = Meta.DELAY_PROFILE)]
    public sealed class RemoveSonarrDelayProfileCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;
        protected override int Capacity => 1;

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSET_PIPELINE)]
        [ValidateIds(ValidateRangeKind.Positive)]
        public DelayProfileObject[] InputObject { get; set; } = Array.Empty<DelayProfileObject>();

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.DELAY_PROFILE];
        }
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.GetReturnables()[0] = _ids;
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
        }
        protected override void Process(IServiceProvider provider)
        {
            if (this.HasParameter(x => x.InputObject) && this.InputObject.Length > 0)
            {
                _ids.UnionWith(this.InputObject.Select(x => x.Id));
            }
        }
        protected override void End(IServiceProvider provider)
        {
            if (_ids.Count <= 0)
            {
                return;
            }

            bool force = this.Force.ToBool();
            foreach (int id in _ids)
            {
                this.DeleteProfile(in id, this.Tag, in force);
            }
        }

        private void DeleteProfile(in int id, MetadataTag tag, in bool force)
        {
            string url = tag.GetUrlForId(id);
            if (!force
                &&
                !this.ShouldProcess(url, "Delete Delay Profile"))
            {
                return;
            }

            var response = this.SendDeleteRequest(url);
            if (response.IsError)
            {
                this.WriteError(response.Error);
                return;
            }

            this.WriteVerbose($"Deleted Delay Profile -> {id}");
        }
    }
}

