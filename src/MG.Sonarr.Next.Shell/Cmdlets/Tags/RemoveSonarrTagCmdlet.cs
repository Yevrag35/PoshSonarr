using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags;

/// <summary>
/// Represents the <c>Remove-SonarrTag</c> cmdlet, which deletes a tag from Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes the tag by <c>-Id</c>, or as a <see cref="TagObject"/> piped to <c>-InputObject</c>, and sends a
/// DELETE request for it. It writes a verbose message when the tag is deleted and nothing to the pipeline, and when the
/// request fails, it writes the error, whether or not it's ignorable.
/// </para>
/// <para>
/// The cmdlet's confirm impact is high, so it asks before each deletion unless <c>-Force</c> or <c>-Confirm:$false</c>
/// is given. <c>-Force</c> skips only that prompt: <c>-WhatIf</c> still shows what would be deleted and deletes
/// nothing, and <c>-Confirm</c> still prompts.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Delete-SonarrTag</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Remove, "SonarrTag", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High,
	DefaultParameterSetName = PSConstants.PSetExplicitId)]
[Alias("Delete-SonarrTag")]
[MetadataCanPipe(Tag = Meta.Tag)]
public sealed class RemoveSonarrTagCmdlet : SonarrApiCmdletBase
{
	private MetadataTag _tag = null!;

	/// <summary>
	/// Gets or sets the ID of the tag to delete.
	/// </summary>
	/// <remarks>
	/// The ID has to be greater than zero. A tag piped to <c>-InputObject</c> sets it to the tag's ID.
	/// </remarks>
	/// <value>The tag's ID.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the tag to delete.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, and the tag's ID has to be greater than zero. Setting it sets
	/// <see cref="Id"/> to the tag's ID, and the getter always returns <see langword="null"/>.
	/// </remarks>
	/// <value>Always <see langword="null"/> when read.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	[ValidateId(ValidateRangeKind.Positive)]
	public TagObject? InputObject
	{
		get => null;
		set => this.Id = value?.Id ?? -1;
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet deletes the tag without asking.
	/// </summary>
	/// <remarks>
	/// The switch skips only the confirmation prompt. <c>-WhatIf</c> and <c>-Confirm</c> still take effect.
	/// </remarks>
	/// <value>The switch, which is present to skip the confirmation prompt.</value>
	[Parameter]
	public SwitchParameter Force { get; set; }

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
	/// Deletes the tag, once the deletion is confirmed or <c>-Force</c> skips the prompt, and writes a verbose message.
	/// </summary>
	/// <remarks>
	/// The confirmation names the tag's endpoint path. A tag whose ID isn't greater than zero is skipped without a
	/// request.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		if (this.Id <= 0)
		{
			return;
		}

		string url = _tag.GetUrlForId(this.Id);
		if (!this.ShouldProcessWithForce(url, "Delete Tag", this.Force))
		{
			return;
		}

		var response = this.SendDeleteRequest(url);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			return;
		}

		this.WriteVerbose($"Deleted Tag -> {this.Id}");
	}
}
