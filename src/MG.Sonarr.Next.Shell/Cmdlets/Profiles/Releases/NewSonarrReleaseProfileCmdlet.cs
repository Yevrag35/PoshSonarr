using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Indexers;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Cmdlets.Tags;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Unions;
using System.Collections;
using System.Text;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Releases
{
    [Cmdlet(VerbsCommon.New, "SonarrReleaseProfile", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    public sealed class NewSonarrReleaseProfileCmdlet : SonarrMetadataCmdlet
    {
        private const string ANY_INDEXER = "Any";

        private SortedSet<int> _tagIds = null!;
        private WildcardSet _tagNames = null!;
        protected override bool CaptureDebugPreference => true;
        public override bool CanDebugSerializeBefore => false;

        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrWhiteSpace]
        public string Name { get; set; } = string.Empty;

        [Parameter]
        public SwitchParameter Enabled { get; set; }

        [Parameter]
        [SupportsWildcards]
        [ValidateId(ValidateRangeKind.Positive, NullBehavior = InputNullBehavior.Ignore)]
        public Either<string, int> Indexer { get; set; } = default;

        [Parameter]
        [Alias("Ignored")]
        [ValidateNotNull]
        [AllowEmptyCollection]
        public string[] IgnoredTerms { get; set; } = [];

        [Parameter]
        [Alias("Required")]
        [ValidateNotNull]
        [AllowEmptyCollection]
        public string[] RequiredTerms { get; set; } = [];

        [Parameter]
        [ValidateNotNull]
        [AllowEmptyCollection]
        [ValidateIds(ValidateRangeKind.Positive, NullBehavior = InputNullBehavior.PassAsZero)]
        public Either<string, int>[] Tags { get; set; } = [];

        protected override int Capacity => 2;


        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.RELEASE_PROFILE];
        }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _tagIds = this.GetPooledObject<SortedSet<int>>();
            _tagNames = this.GetPooledObject<WildcardSet>();
            this.SetReturnables(_tagIds, _tagNames);
        }

        protected override void Begin(IServiceProvider provider)
        {
            if (this.HasParameter(this.Tags))
            {
                this.Tags.SplitToSets(_tagIds, _tagNames, explicitlyCalledForString: false);
            }

            if (_tagNames.Count > 0)
            {
                MetadataTag tag = provider.GetMetadataTag(Meta.TAG);
                this.ProcessNames(_tagNames, _tagIds, tag);
            }

            if (this.HasParameter(this.Indexer) && IsIndexNameAndNotAny(this.Indexer, out Wildcard indexerName))
            {
                MetadataTag tag = provider.GetMetadataTag(Meta.INDEXER);
                var indexers = this.GetAll<IndexerObject>(tag.UrlBase);
                if (indexers.Count == 0)
                {
                    return;
                }

                IndexerObject? indexer = indexers.Find(x => indexerName.IsMatch(x.Name));
                if (indexer is not null)
                {
                    this.WriteVerbose($"Found indexer from name -> {indexer.Name} ({indexer.Id})");
                    this.Indexer = indexer.Id;
                }
                else
                {
                    this.Error = new SonarrErrorRecord(new ArgumentException($"No indexer found the name '{indexerName}'.",
                        nameof(this.Indexer)),
                        "SonarrObjectNotMatchedToName",
                        ErrorCategory.ObjectNotFound,
                        indexerName);
                }
            }
        }

        [SuppressMessage("Style", "IDE0037:Use inferred member name", Justification = "Naming should not be tied to Cmdlet parameter names.")]
        protected override void Process(IServiceProvider provider)
        {
            var body = new
            {
                Name = this.Name,
                Enabled = this.Enabled.ToBool(),
                IndexerId = this.Indexer.AsT2,
                Tags = _tagIds,
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

            this.WriteOutcome(response);
        }

        private static bool IsIndexNameAndNotAny(Either<string, int> indexerInfo, out Wildcard indexerName)
        {
            if (!indexerInfo.TryGetT1(out string? name, out _))
            {
                indexerName = Wildcard.Empty;
                return false;
            }

            indexerName = Wildcard.Parse(name);
            if (indexerName.IsEmpty || indexerName.MatchType == WildcardMatchType.All)
            {
                return false;
            }

            bool result = !indexerName.Equals(ANY_INDEXER, StringComparison.OrdinalIgnoreCase);
            if (!result && indexerName.MatchType == WildcardMatchType.Exact && indexerName.Length == ANY_INDEXER.Length + 2)
            {
                result = !indexerName.AsSpan().EnclosedIn('(', ')');
            }

            return result;
        }
        private void ProcessNames(WildcardSet names, SortedSet<int> tagIds, MetadataTag tag)
        {
            if (names.Count == 0)
            {
                return;
            }

            var tags = this.GetAll<TagObject>(tag.UrlBase).AsSpan();

            for (int i = 0; i < tags.Length; i++)
            {
                TagObject tagObj = tags[i];
                if (!tagIds.Contains(tagObj.Id) && names.IsAnyMatch(tagObj.Label))
                {
                    tagIds.Add(tagObj.Id);
                }
            }
        }
    }
}

