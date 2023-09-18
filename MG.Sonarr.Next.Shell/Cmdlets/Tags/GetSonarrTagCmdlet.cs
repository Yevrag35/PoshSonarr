using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
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

        private void AddArrayToSet(object[]? array)
        {
            if (array is not null)
            {
                foreach (object item in array)
                {
                    if (item.IsCorrectType("Tag", out var pso) && pso.TryGetProperty(nameof(this.Id), out int id))
                    {
                        _ = _ids.Add(id);
                    }
                    else if (item.TryGetProperty("Tags", out Array? idArray))
                    {
                        foreach (int tagId in idArray)
                        {
                            _ = _ids.Add(tagId);
                        }
                    }
                }

                if (_ids.Count <= 0)
                {
                    this.Error =
                        new ArgumentException("Unable to find any valid tag ID's in the pipeline input.")
                            .ToRecord(ErrorCategory.InvalidArgument, array);
                }
            }
        }

        protected override ErrorRecord? End()
        {
            bool hasIds = false;
            if (_ids.Count > 0)
            {
                hasIds = true;
                foreach (int id in _ids)
                {
                    var result = this.SendGetRequest<object>($"/tag/{id}");
                    if (!result.IsError)
                    {
                        result.Data.AddMetadataTag("Tag");
                    }

                    this.WriteSonarrResult(result);
                }
            }

            if (_names.Count > 0)
            {
                var list = this.GetAllTags();
                if (list.IsError)
                {
                    return list.Error;
                }

                for (int i = list.Data.Count - 1; i >= 0; i--)
                {
                    object item = list.Data[i];
                    if (!item.TryGetProperty("Label", out string? label)
                        ||
                        !_names.ValueLike(label))
                    {
                        list.Data.RemoveAt(i);
                    }
                    else
                    {
                        item.AddMetadataTag("Tag");
                    }
                }

                this.WriteSonarrResult(list);
            }
            else if (!hasIds)
            {
                var tags = this.GetAllTags();
                if (tags.IsError)
                {
                    return tags.Error;
                }

                tags.Data.ForEach(x => x.AddMetadataTag("Tag"));

                this.WriteSonarrResult(tags);
            }

            return null;
        }

        private SonarrResponse<List<object>> GetAllTags()
        {
            return this.SendGetRequest<List<object>>("/tag");
        }
    }
}
