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

/// <summary>
/// Represents the <c>New-SonarrReleaseProfile</c> cmdlet, which creates a release profile in Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet sends Sonarr a new release profile built from its parameters, and writes the profile that Sonarr created
/// to the pipeline as a <see cref="ReleaseProfileObject"/>. It takes no pipeline input.
/// </para>
/// <para>
/// <c>-RequiredTerms</c> and <c>-IgnoredTerms</c> set the terms that a release has to contain or mustn't contain,
/// <c>-Indexer</c> limits the profile to one indexer, and <c>-Tags</c> names the tags of the series that the profile
/// applies to. When finding the tags or the indexer fails, or a tag name or the indexer's name matches nothing, the
/// cmdlet stops with an error and creates no profile.
/// </para>
/// <para>
/// The cmdlet confirms before it creates the profile, and its confirm impact is low, so it prompts only when
/// <c>-Confirm</c> or <c>$ConfirmPreference</c> asks it to.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.New, "SonarrReleaseProfile", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
public sealed class NewSonarrReleaseProfileCmdlet : SonarrMetadataCmdlet
{
	private const string ANY_INDEXER = "Any";

	private SortedSet<int> _tagIds = null!;
	private WildcardSet _tagNames = null!;
	/// <summary>
	/// Gets a value that indicates whether the cmdlet captures its debug preference before processing begins.
	/// </summary>
	/// <remarks>
	/// The cmdlet reads the preference to decide whether to write its request body to the debug stream.
	/// </remarks>
	/// <value>Always <see langword="true"/>.</value>
	protected override bool CaptureDebugPreference => true;
	/// <summary>
	/// Gets a value that indicates whether the request's JSON body is written to the debug stream before the request is
	/// sent.
	/// </summary>
	/// <remarks>
	/// The cmdlet writes the body to the debug stream itself, through
	/// <see cref="SonarrCmdletBase.SerializeIfDebug(object?, string?, bool, System.Text.Json.JsonSerializerOptions?)"/>,
	/// so the property returns <see langword="false"/> and the body appears once.
	/// </remarks>
	/// <value>Always <see langword="false"/>.</value>
	public override bool CanDebugSerializeBefore => false;

	/// <summary>
	/// Gets or sets the name of the release profile.
	/// </summary>
	/// <remarks>
	/// The name can't be empty or white space.
	/// </remarks>
	/// <value>The profile's name.</value>
	[Parameter(Mandatory = true, Position = 0)]
	[ValidateNotNullOrWhiteSpace]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets a value that indicates whether the profile is enabled.
	/// </summary>
	/// <value>The switch, which is present to enable the profile.</value>
	[Parameter]
	public SwitchParameter Enabled { get; set; }

	/// <summary>
	/// Gets or sets the ID or name of the indexer that the profile applies to.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A number is the indexer's ID, which has to be greater than zero. <c>Any</c>, <c>(Any)</c>, and <c>*</c>, ignoring
	/// case, mean every indexer. Any other text is a name, which can hold wildcards, and the profile takes the ID of the
	/// first indexer whose name matches it.
	/// </para>
	/// <para>
	/// When the request for the indexers fails, or no indexer's name matches, the cmdlet stops with an error and creates
	/// no profile.
	/// </para>
	/// </remarks>
	/// <value>The indexer's ID or name. The default holds neither, for every indexer.</value>
	[Parameter]
	[SupportsWildcards]
	[ValidateId(ValidateRangeKind.Positive, NullBehavior = InputNullBehavior.Ignore)]
	public Either<string, int> Indexer { get; set; } = default;

	/// <summary>
	/// Gets or sets the terms that a release mustn't contain.
	/// </summary>
	/// <remarks>
	/// The parameter's alias is <c>-Ignored</c>.
	/// </remarks>
	/// <value>The ignored terms. The default is an empty array.</value>
	[Parameter]
	[Alias("Ignored")]
	[ValidateNotNull]
	[AllowEmptyCollection]
	public string[] IgnoredTerms { get; set; } = [];

	/// <summary>
	/// Gets or sets the terms that a release has to contain.
	/// </summary>
	/// <remarks>
	/// The parameter's alias is <c>-Required</c>.
	/// </remarks>
	/// <value>The required terms. The default is an empty array.</value>
	[Parameter]
	[Alias("Required")]
	[ValidateNotNull]
	[AllowEmptyCollection]
	public string[] RequiredTerms { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs and names of the tags of the series that the profile applies to.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A number, or text that reads as a whole number, is a tag's ID, which has to be greater than zero. Any other text
	/// is a name, which can hold wildcards and matches a tag's label ignoring case.
	/// </para>
	/// <para>
	/// Each name has to match at least one tag's label, since the profile would otherwise go out without the tag that
	/// the name meant, and a profile left without tags applies to every series. When a name matches no tag, or the
	/// request for the tags fails, the cmdlet stops with an error and creates no profile.
	/// </para>
	/// </remarks>
	/// <value>The IDs and names of the tags. The default is an empty array, for a profile without tags.</value>
	[Parameter]
	[ValidateNotNull]
	[AllowEmptyCollection]
	[ValidateIds(ValidateRangeKind.Positive, NullBehavior = InputNullBehavior.Ignore)]
	public Either<string, int>[] Tags { get; set; } = [];

	/// <summary>
	/// Gets the metadata tag of the release profile endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The release profile's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.ReleaseProfile];
	}

	/// <summary>
	/// Runs the base class's setup for the scope, then creates the sets of tag IDs and names that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_tagIds = [];
		_tagNames = new();
	}

	/// <summary>
	/// Finds the IDs of the tags whose labels match the names in <c>-Tags</c>, and the ID of the indexer that a name in
	/// <c>-Indexer</c> names.
	/// </summary>
	/// <remarks>
	/// Each lookup gets every tag or every indexer with one request. When a request fails, a tag name matches no tag, or
	/// no indexer's name matches, the cmdlet stops with an error, so <see cref="Process(IServiceProvider)"/> doesn't run.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
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

	/// <summary>
	/// Builds the profile's request body from the parameters, writes it to the debug stream, and sends it to Sonarr once
	/// the action is confirmed.
	/// </summary>
	/// <remarks>
	/// The body is an anonymous object, not a model, so writing it to the debug stream before the confirmation changes
	/// nothing that <c>-WhatIf</c> would have to undo. Its indexer ID is 0, which means every indexer, unless
	/// <c>-Indexer</c> gave or found one.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
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

	/// <summary>
	/// Posts the specified body to the release profile endpoint, and writes the profile that Sonarr created to the
	/// pipeline, or the request's error unless it's ignorable.
	/// </summary>
	/// <param name="body">The new profile's request body.</param>
	private void CreateProfile(object body)
	{
		Either<ReleaseProfileObject, SonarrErrorRecord> response = this.SendPostRequest<ReleaseProfileObject>(this.Tag.UrlBase, body);

		this.WriteOutcome(response);
	}

	// Determines whether -Indexer holds a name to look up. 'Any', '(Any)', and '*' mean every indexer, which the body
	// sends as the indexer ID 0. Any other text, empty text included, names an indexer to find.
	/// <summary>
	/// Determines whether the specified value of <c>-Indexer</c> is a name of an indexer to find, rather than an ID or
	/// one of the values that mean every indexer.
	/// </summary>
	/// <remarks>
	/// <c>Any</c> and <c>(Any)</c>, ignoring case, and a wildcard that matches everything, such as <c>*</c>, mean every
	/// indexer. Any other text, empty text included, is a name.
	/// </remarks>
	/// <param name="indexerInfo">The value of <c>-Indexer</c>.</param>
	/// <param name="indexerName">
	/// When the method returns, the text parsed as a wildcard, or <see cref="Wildcard.Empty"/> when the value isn't text.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="indexerInfo"/> is a name to find; otherwise, <see langword="false"/>.
	/// </returns>
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
	/// <summary>
	/// Adds to the specified set the IDs of the tags whose labels match one of the specified names, and stops the cmdlet
	/// with an error when a name matches no tag.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The method gets every tag with one request, and when that request fails, the cmdlet stops with its error.
	/// </para>
	/// <para>
	/// Each name is matched against every tag's label, ignoring case, so a name counts as matched even when the tag it
	/// matches was also given by ID. The error for the names that match no tag names each of them, and its target is
	/// their text.
	/// </para>
	/// </remarks>
	/// <param name="names">The names, which can hold wildcards, that the tags' labels are matched against.</param>
	/// <param name="tagIds">The tag IDs that the cmdlet sends, which the method adds the matching tags' IDs to.</param>
	/// <param name="tag">The metadata tag of the tag endpoint.</param>
	private void ProcessNames(WildcardSet names, SortedSet<int> tagIds, MetadataTag tag)
	{
		if (names.Count == 0)
		{
			return;
		}

		MetadataList<TagObject> tags = this.GetAll<TagObject>(tag.UrlBase);
		if (this.IsStopped)
		{
			return;
		}

		List<string>? unmatched = null;
		foreach (Wildcard name in names)
		{
			bool matched = false;
			foreach (var tagObj in tags.AsSpan())
			{
				if (name.IsMatch(tagObj.Label))
				{
					_ = tagIds.Add(tagObj.Id);
					matched = true;
				}
			}

			if (!matched)
			{
				(unmatched ??= []).Add(name.ToString());
			}
		}

		if (unmatched is not null)
		{
			this.StopCmdlet(new SonarrErrorRecord(
				normalEx: new SonarrParameterException(nameof(this.Tags), ParameterErrorType.Invalid,
					$"No tag was found with a label that matches '{string.Join("' or '", unmatched)}'."),
				"New-SonarrReleaseProfile.InvalidTagName",
				ErrorCategory.InvalidArgument,
				unmatched.ToArray()));
		}
	}
}

