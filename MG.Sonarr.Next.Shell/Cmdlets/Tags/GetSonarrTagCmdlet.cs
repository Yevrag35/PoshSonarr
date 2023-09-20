using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Metadata;
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
        readonly MetadataTag _tag;
        MetadataResolver Resolver { get; }

        public GetSonarrTagCmdlet()
            : base()
        {
            _ids = new SortedSet<int>();
            _names = new SortedSet<WildcardString>();
            this.Resolver = this.Services.GetRequiredService<MetadataResolver>();
            _tag = this.Resolver[Meta.TAG];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipelineInput")]
        public object[] InputObject
        {
            get => Array.Empty<object>();
            set => this.AddArrayToSet(value);
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

        protected override ErrorRecord? End()
        {
            bool hasIds = this.ProcessIds(_ids);

            if (!this.TryProcessNames(_names) && !hasIds)
            {
                SonarrResponse<List<PSObject>> tags = this.GetAllTags();
                if (tags.IsError)
                {
                    return tags.Error;
                }

                foreach (PSObject item in tags.Data)
                {
                    item.AddMetadata(_tag);
                }

                this.WriteSonarrResult(tags);
            }

            return null;
        }

        private void AddArrayToSet(object[]? array)
        {
            if (array is null)
            {
                return;
            }

            this.GetNamesAndIdsFromAdd(array);

            if (_ids.Count <= 0)
            {
                this.Error =
                    new ArgumentException("Unable to find any valid tag ID's in the pipeline input.")
                        .ToRecord(ErrorCategory.InvalidArgument, array);
            }
        }
        private SonarrResponse<List<PSObject>> GetAllTags()
        {
            return this.SendGetRequest<List<PSObject>>(Constants.TAG);
        }
        private void GetNamesAndIdsFromAdd(object[] array)
        {
            foreach (object item in array)
            {
                if (item.IsCorrectType(Meta.TAG, out var pso)
                    &&
                    pso.TryGetProperty(nameof(this.Id), out int id))
                {
                    _ = _ids.Add(id);
                }
                else if (item.TryGetProperty("Tags", out IEnumerable<int>? idArray))
                {
                    _ids.UnionWith(idArray);
                }
            }
        }
        private static void ProcessAndFilterTags(List<PSObject> data, MetadataTag tagToAdd, IReadOnlySet<WildcardString> names)
        {
            for (int i = data.Count - 1; i >= 0; i--)
            {
                object item = data[i];
                if (!item.TryGetProperty(Constants.LABEL, out string? label)
                    ||
                    !names.ValueLike(label))
                {
                    data.RemoveAt(i);
                }
                else
                {
                    item.AddMetadata(tagToAdd);
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
                    var result = this.SendGetRequest<PSObject>($"/tag/{id}");
                    if (!result.IsError)
                    {
                        result.Data.AddMetadata(_tag);
                        this.WriteObject(result.Data, enumerateCollection: true);
                    }
                    else
                    {
                        this.WriteError(result.Error);
                        this.Error = result.Error;
                    }
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

            SonarrResponse<List<PSObject>> list = this.GetAllTags();
            if (list.IsError)
            {
                this.Error = list.Error;
            }
            else
            {
                ProcessAndFilterTags(list.Data, _tag, _names);
                this.WriteSonarrResult(list);
            }
            
            return true;
        }
    }
}
