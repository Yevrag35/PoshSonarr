using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Components;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags;

/// <summary>
/// Represents the <c>Rename-SonarrTag</c> cmdlet, which gives a tag in Sonarr a new label.
/// </summary>
/// <remarks>
/// The cmdlet takes the tag by <c>-Id</c>, or as a <see cref="TagObject"/> piped to <c>-InputObject</c>. It confirms
/// the change, naming the tag's endpoint path and the new label, and sends a PUT request whose body is a
/// <see cref="TagRename"/> with the tag's ID and the new label. It writes a verbose message when the tag is renamed
/// and nothing to the pipeline, and when the request fails, it writes the error, whether or not it's ignorable.
/// </remarks>
[Cmdlet(VerbsCommon.Rename, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
	DefaultParameterSetName = PSConstants.PSetExplicitId)]
[MetadataCanPipe(Tag = Meta.Tag)]
public sealed class RenameSonarrTagCmdlet : SonarrApiCmdletBase
{
	private TagObject? _pipedObject;
	private MetadataTag _tag = null!;

	/// <summary>
	/// Gets or sets the ID of the tag to rename.
	/// </summary>
	/// <remarks>
	/// The ID has to be greater than zero. A tag piped to <c>-InputObject</c> sets it to the tag's ID.
	/// </remarks>
	/// <value>The tag's ID.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the tag to rename.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, and the tag's ID has to be greater than zero. Setting it to a tag sets
	/// <see cref="Id"/> to the tag's ID, and setting it to <see langword="null"/> changes nothing.
	/// </remarks>
	/// <value>The last tag that was set, or <see langword="null"/> when none was.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	[ValidateId(ValidateRangeKind.Positive)]
	public TagObject? InputObject
	{
		get => _pipedObject;
		set
		{
			if (value is not null)
			{
				_pipedObject = value;
				this.Id = value.Id;
			}
		}
	}

	/// <summary>
	/// Gets or sets the tag's new label.
	/// </summary>
	/// <remarks>
	/// The label can't be <see langword="null"/> or empty. With piped input, the parameter also binds from a piped
	/// object's <c>NewName</c> property.
	/// </remarks>
	/// <value>The new label. The default is an empty string.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetPipeline, ValueFromPipelineByPropertyName = true)]
	[Parameter(Mandatory = true, Position = 1, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateNotNullOrEmpty]
	public string NewName { get; set; } = string.Empty;

	/// <summary>
	/// Runs the base class's setup for the scope, then gets the metadata tag of the tag endpoint.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_tag = provider.GetRequiredService<IMetadataResolver>()[Meta.Tag];
	}

	/// <summary>
	/// Renames the tag, once the change is confirmed, and writes a verbose message.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		TagRename rename = TagRename.Create(this.Id, this.NewName);

		string url = _tag.GetUrlForId(rename.Id);

		if (this.ShouldProcess(url, $"Renaming Tag -> {rename.Label}"))
		{
			SonarrClientResult response = this.SendPutRequest(url, rename);

			if (response.IsError)
			{
				this.WriteError(response.Error);
				return;
			}

			this.WriteVerbose($"Renamed Tag -> {this.Id}");
		}
	}
}
