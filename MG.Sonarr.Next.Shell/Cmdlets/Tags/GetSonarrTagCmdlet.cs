using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Tags;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OneOf.Types;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Get, "SonarrTag", DefaultParameterSetName = "ByName")]
    public sealed class GetSonarrTagCmdlet : SonarrApiCmdletBase
    {
        readonly SortedSet<int> _ids;
        readonly SortedSet<WildcardString> _names;

        public GetSonarrTagCmdlet()
            : base()
        {
            _ids = new SortedSet<int>();
            _names = new SortedSet<WildcardString>();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipelineInput")]
        public ITagPipeable[] InputObject
        {
            get => Array.Empty<ITagPipeable>();
            set => _ids.UnionWith(value.SelectMany(x => x.Tags));
        }


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ById")]
        public int[] Id
        {
            get => Array.Empty<int>();
            set => _ids.UnionWith(value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByName")]
        [SupportsWildcards]
        public IntOrString[] Name
        {
            get => Array.Empty<IntOrString>();
            set => value.SplitToSets(_ids, _names);
        }

        protected override void End()
        {
            bool hasIds = this.ProcessIds(_ids);

            if (!this.TryProcessNames(_names) && !hasIds)
            {
                SonarrResponse<MetadataList<TagObject>> tags = this.GetAllTags();
                if (tags.IsError)
                {
                    this.StopCmdlet(tags.Error);
                    return;
                }

                this.WriteCollection(tags.Data);
            }
        }
        private SonarrResponse<MetadataList<TagObject>> GetAllTags()
        {
            return this.SendGetRequest<MetadataList<TagObject>>(Constants.TAG);
        }
        private static void ProcessAndFilterTags(IList<TagObject> data, IReadOnlySet<WildcardString> names)
        {
            for (int i = data.Count - 1; i >= 0; i--)
            {
                if (!names.AnyValueLike(data[i].Label))
                {
                    data.RemoveAt(i);
                }
            }
        }
        private bool ProcessIds(IReadOnlyCollection<int> ids)
        {
            bool hasIds = false;
            if (ids.Count > 0)
            {
                hasIds = true;
                foreach (int id in ids)
                {
                    var result = this.SendGetRequest<TagObject>($"/tag/{id}");
                    if (result.IsError)
                    {
                        this.WriteConditionalError(result.Error);
                        continue;
                    }

                    this.WriteObject(result.Data, enumerateCollection: true);
                }
            }

            return hasIds;
        }
        private bool TryProcessNames(IReadOnlySet<WildcardString> names)
        {
            if (names.Count <= 0)
            {
                return false;
            }

            SonarrResponse<MetadataList<TagObject>> list = this.GetAllTags();
            if (list.IsError)
            {
                this.StopCmdlet(list.Error);
                return false;
            }
            else
            {
                ProcessAndFilterTags(list.Data, _names);
                this.WriteCollection(list.Data);
            }
            
            return true;
        }
    }
}
