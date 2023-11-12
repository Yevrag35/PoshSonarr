using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Commands;
using MG.Sonarr.Next.Services.Jobs;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Output;

namespace MG.Sonarr.Next.Shell.Cmdlets.Commands
{
    [Cmdlet(VerbsCommon.Get, "SonarrCommand", DefaultParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
    [MetadataCanPipe(Tag = Meta.COMMAND)]
    [OutputType(typeof(ICommandOutput))]
    public sealed class GetSonarrCommandCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSET_PIPELINE)]
        public ICommand[] InputObject { get; set; } = Array.Empty<ICommand>();

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

        protected override int Capacity => 1;
        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.COMMAND];
        }
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.GetReturnables()[0] = _ids;
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id ?? Array.Empty<int>());
        }
        protected override void Process(IServiceProvider provider)
        {
            if (this.HasParameter(x => x.InputObject))
            {
                _ids.UnionWith(this.InputObject.Select(x => x.Id));
            }
        }
        protected override void End(IServiceProvider provider)
        {
            ICommandHistory history = provider.GetRequiredService<ICommandHistory>();

            foreach (int id in _ids)
            {
                string url = this.Tag.GetUrlForId(id);
                var response = this.SendGetRequest<CommandObject>(url);

                if (this.TryWriteObject(in response) && history.Remove(id))
                {
                    history.Add(response.Data);
                }
            }
        }
    }
}
