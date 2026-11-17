using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Exceptions.Shell;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Delay;

/// <summary>
/// Represents the <c>New-SonarrDelayProfile</c> cmdlet, which creates a delay profile in Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet sends Sonarr a new delay profile built from its parameters, and writes the profile that Sonarr created
/// to the pipeline as a <see cref="DelayProfileObject"/>. It takes no pipeline input.
/// </para>
/// <para>
/// <c>-Protocol</c> sets which of Usenet and torrents the profile allows and which one it prefers, and
/// <c>-UsenetDelay</c> and <c>-TorrentDelay</c> set how long Sonarr waits before grabbing a release of each, in whole
/// minutes. <c>-Tags</c> names the tags of the series that the profile applies to. When finding the tags fails, or a
/// tag name matches no tag, the cmdlet stops with an error and creates no profile.
/// </para>
/// <para>
/// The cmdlet confirms before it creates the profile, and its confirm impact is low, so it prompts only when
/// <c>-Confirm</c> or <c>$ConfirmPreference</c> asks it to.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.New, "SonarrDelayProfile", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
public sealed class NewSonarrDelayProfileCmdlet : SonarrMetadataCmdlet
{
	/// <summary>
	/// Gets or sets the profile's place in the order in which Sonarr tries the delay profiles.
	/// </summary>
	/// <remarks>
	/// The value has to be at least 1.
	/// </remarks>
	/// <value>The profile's order.</value>
	[Parameter(Mandatory = true), ValidateRange(1, int.MaxValue)]
	public int Order { get; set; }

	/// <summary>
	/// Gets or sets which download protocols the profile allows and which one it prefers.
	/// </summary>
	/// <value>The profile's protocols.</value>
	[Parameter(Mandatory = true)]
	public PreferredProtocol Protocol { get; set; }

	/// <summary>
	/// Gets or sets how long Sonarr waits before it grabs a torrent release.
	/// </summary>
	/// <remarks>
	/// The cmdlet sends the delay in whole minutes and drops any part of a minute.
	/// </remarks>
	/// <value>The torrent delay. The default is <see cref="TimeSpan.Zero"/>, for no delay.</value>
	[Parameter]
	public TimeSpan TorrentDelay { get; set; }

	/// <summary>
	/// Gets or sets how long Sonarr waits before it grabs a Usenet release.
	/// </summary>
	/// <remarks>
	/// The cmdlet sends the delay in whole minutes and drops any part of a minute.
	/// </remarks>
	/// <value>The Usenet delay. The default is <see cref="TimeSpan.Zero"/>, for no delay.</value>
	[Parameter]
	public TimeSpan UsenetDelay { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether Sonarr skips the delay for a release in the highest quality that the
	/// series' quality profile allows.
	/// </summary>
	/// <value>The switch, which is present to skip the delay for a release in the highest quality.</value>
	[Parameter]
	public SwitchParameter BypassIfHighestQuality { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether Sonarr skips the delay for a release whose custom format score reaches
	/// <c>-MinimumCustomFormatScore</c>.
	/// </summary>
	/// <value>The switch, which is present to skip the delay for a release with a high enough score.</value>
	[Parameter]
	public SwitchParameter BypassIfAboveCustomFormatScore { get; set; }

	/// <summary>
	/// Gets or sets the custom format score at which <c>-BypassIfAboveCustomFormatScore</c> skips the delay.
	/// </summary>
	/// <value>The minimum custom format score. The default is 0.</value>
	[Parameter]
	public int MinimumCustomFormatScore { get; set; }

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
	/// the name meant. When a name matches no tag, or the request for the tags fails, the cmdlet stops with an error and
	/// creates no profile.
	/// </para>
	/// </remarks>
	/// <value>The IDs and names of the tags. The default is an empty array, for a profile without tags.</value>
	[Parameter, ValidateNotNull, AllowEmptyCollection, ValidateIds(ValidateRangeKind.Positive, NullBehavior = InputNullBehavior.Ignore)]
	public Either<string, int>[] Tags { get; set; } = [];

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

	private SortedSet<int> _tagIds = null!;
	private WildcardSet _tagNames = null!;

	/// <summary>
	/// Gets the metadata tag of the delay profile endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The delay profile's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.DelayProfile];
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
	/// Splits <c>-Tags</c> into IDs and names, and finds the IDs of the tags whose labels match the names.
	/// </summary>
	/// <remarks>
	/// Matching the names gets every tag with one request. When that request fails, or a name matches no tag, the cmdlet
	/// stops with an error, so <see cref="Process(IServiceProvider)"/> doesn't run.
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
		}
	}

	/// <summary>
	/// Builds the profile's request body from the parameters, writes it to the debug stream, and sends it to Sonarr once
	/// the action is confirmed.
	/// </summary>
	/// <remarks>
	/// The body is an anonymous object, not a model, so writing it to the debug stream before the confirmation changes
	/// nothing that <c>-WhatIf</c> would have to undo.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		var body = new
		{
			EnableUsenet = this.Protocol is not PreferredProtocol.OnlyTorrent,
			EnableTorrent = this.Protocol is not PreferredProtocol.OnlyUsenet,
			BypassIfHighestQuality = this.BypassIfHighestQuality.ToBool(),
			BypassIfAboveCustomFormatScore = this.BypassIfAboveCustomFormatScore.ToBool(),
			MinimumCustomFormatScore = this.MinimumCustomFormatScore,
			Tags = _tagIds,
			Order = this.Order,
			UsenetDelay = (int)this.UsenetDelay.TotalMinutes,
			TorrentDelay = (int)this.TorrentDelay.TotalMinutes,
			PreferredProtocol = this.Protocol is PreferredProtocol.OnlyTorrent or PreferredProtocol.PreferTorrent ? "torrent" : "usenet",
		};

		this.SerializeIfDebug(body, includeType: false);

		if (this.ShouldProcess(this.Tag.UrlBase, $"Create new delay profile with order -> {this.Order}"))
		{
			this.CreateProfile(body);
		}
	}

	/// <summary>
	/// Posts the specified body to the delay profile endpoint, and writes the profile that Sonarr created to the
	/// pipeline, or the request's error unless it's ignorable.
	/// </summary>
	/// <param name="body">The new profile's request body.</param>
	private void CreateProfile(object body)
	{
		var response = this.SendPostRequest<DelayProfileObject>(this.Tag.UrlBase, body);
		this.WriteOutcome(response);
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
				"New-SonarrDelayProfile.InvalidTagName",
				ErrorCategory.InvalidArgument,
				unmatched.ToArray()));
		}
	}
}

/// <summary>
/// Specifies which download protocols a delay profile allows and which one it prefers.
/// </summary>
public enum PreferredProtocol
{
	/// <summary>
	/// Both protocols are allowed, and torrents are preferred.
	/// </summary>
	PreferTorrent,
	/// <summary>
	/// Both protocols are allowed, and Usenet is preferred.
	/// </summary>
	PreferUsenet,
	/// <summary>
	/// Only torrents are allowed.
	/// </summary>
	OnlyTorrent,
	/// <summary>
	/// Only Usenet is allowed.
	/// </summary>
	OnlyUsenet,
};
