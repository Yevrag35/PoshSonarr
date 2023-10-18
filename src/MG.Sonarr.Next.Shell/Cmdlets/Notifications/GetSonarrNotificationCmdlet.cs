using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Notifications;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Notifications
{
    [Cmdlet(VerbsCommon.Get, "SonarrNotification", DefaultParameterSetName = "None")]
    public sealed class GetSonarrNotificationCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;
        HashSet<Wildcard> _wcNames = null!;

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ById")]
        public int[] Id { get; set; } = Array.Empty<int>();

        [Parameter(Position = 0)]
        public string[] Name { get; set; } = Array.Empty<string>();

        protected override int Capacity => 2;
        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.NOTIFICATION];
        }
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            _wcNames = this.GetPooledObject<HashSet<Wildcard>>();

            this.Returnables[0] = _ids;
            this.Returnables[1] = _wcNames;
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
            if (this.HasParameter(x => x.Name))
            {
                _wcNames.UnionWith(this.Name);
            }
        }

        protected override void Process(IServiceProvider provider)
        {
            bool gotIds = false;
            if (_ids.Count > 0)
            {
                gotIds = true;
                var fromIds = this.GetById<NotificationObject>(_ids);
                this.WriteCollection(fromIds);
            }

            if (_wcNames.Count > 0 || !gotIds)
            {
                var fromNames = this.GetByName(_wcNames, _ids);
                this.WriteCollection(fromNames);
            }
        }

        private IEnumerable<NotificationObject> GetByName(IReadOnlySet<Wildcard> names, IReadOnlySet<int> ids)
        {
            var response = this.GetAll<NotificationObject>();
            if (response.Count <= 0 || names.Count <= 0)
            {
                return response;
            }

            for (int i = response.Count - 1; i >= 0; i--)
            {
                var item = response[i];
                if (ids.Contains(item.Id) || !names.AnyValueLike(item.Name))
                {
                    response.RemoveAt(i);
                }
            }

            return response;
        }
    }
}
