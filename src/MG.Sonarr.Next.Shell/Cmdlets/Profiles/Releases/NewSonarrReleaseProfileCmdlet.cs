using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Exceptions.Shell;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Indexers;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Releases;

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
	[ValidateIds(ValidateRangeKind.Positive, NullBehavior = InputNullBehavior.Ignore)]
	public Either<string, int>[] Tags { get; set; } = [];

	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.ReleaseProfile];
	}

	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_tagIds = [];
		_tagNames = new();
	}

	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
	protected override void Begin(IServiceProvider provider)
	{
		if (this.HasParameter(this.Tags))
		{
			this.Tags.SplitToSets(_tagIds, _tagNames, explicitlyCalledForString: false);
		}

		if (_tagNames.Count > 0)
		{
			MetadataTag tag = provider.GetMetadataTag(Meta.Tag);
			this.ProcessNames(_tagNames, _tagIds, tag);
			if (this.IsStopped)
			{
				return;
			}
		}

		if (this.HasParameter(this.Indexer) && IsIndexNameAndNotAny(this.Indexer, out Wildcard indexerName))
		{
			// A name that matches no indexer stops the cmdlet with its error, whether or not the server has any
			// indexers, since the body would otherwise go out with the indexer ID 0, which means every indexer.
			MetadataTag tag = provider.GetMetadataTag(Meta.Indexer);
			SonarrClientResult<MetadataList<IndexerObject>> response = this.SendGetRequest<MetadataList<IndexerObject>>(tag.UrlBase);
			if (response.IsError)
			{
				this.StopCmdlet(response.Error);
				return;
			}

			if (response.Value.TryFind(indexerName, static (x, name) => name.IsMatch(x.Name), out var indexer))
			{
				this.WriteVerbose($"Found indexer from name -> {indexer.Name} ({indexer.Id})");
				this.Indexer = indexer.Id;
			}
			else
			{
				this.StopCmdlet(new SonarrErrorRecord(
					normalEx: new SonarrParameterException(nameof(Indexer), ParameterErrorType.Invalid, $"No indexer was found with the name '{indexerName}'."),
					"New-SonarrReleaseProfile.InvalidIndexerName",
					ErrorCategory.InvalidArgument,
					indexerName));
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

	private void CreateProfile(object body)
	{
		Either<ReleaseProfileObject, SonarrErrorRecord> response = this.SendPostRequest<ReleaseProfileObject>(this.Tag.UrlBase, body);

		this.WriteOutcome(response);
	}

	// Determines whether -Indexer holds a name to look up. 'Any', '(Any)', and '*' mean every indexer, which the body
	// sends as the indexer ID 0. Any other text, empty text included, names an indexer to find.
	private static bool IsIndexNameAndNotAny(Either<string, int> indexerInfo, out Wildcard indexerName)
	{
		if (!indexerInfo.TryGetT1(out string? name, out _))
		{
			indexerName = Wildcard.Empty;
			return false;
		}

		indexerName = Wildcard.Parse(name);
		if (indexerName.MatchType == WildcardMatchType.All && !indexerName.IsEmpty)
		{
			return false;
		}

		ReadOnlySpan<char> text = name;
		if (text.Length == ANY_INDEXER.Length + 2 && text.EnclosedIn('(', ')'))
		{
			text = text[1..^1];
		}

		return !text.Equals(ANY_INDEXER, StringComparison.OrdinalIgnoreCase);
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

