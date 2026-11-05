using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes;

/// <summary>
/// Represents the <c>Remove-SonarrEpisodeFile</c> cmdlet, which deletes episode files from Sonarr and from disk.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes the files' IDs through <c>-Id</c>, or episodes, episode files, or calendar entries piped to it,
/// and collects them until its input ends, deleting each file once. An episode or a calendar entry without a file
/// gets a warning instead. Then the cmdlet sends a delete request for each file that's confirmed, and writes nothing
/// to the pipeline. When Sonarr doesn't have a file, the cmdlet writes a warning, and it writes the error of any other
/// request that fails and goes on to the next file.
/// </para>
/// <para>
/// The cmdlet asks <c>ShouldProcess</c> for each file, which prompts when <c>-Confirm</c> or
/// <c>$ConfirmPreference</c> asks for it, and then always asks again with a prompt of its own. <c>-Force</c> skips
/// both prompts, unless <c>-Confirm</c> is given too, which brings back the first. <c>-WhatIf</c> shows what the
/// cmdlet would delete and deletes nothing, with or without <c>-Force</c>.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Delete-SonarrEpisodeFile</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Remove, "SonarrEpisodeFile", SupportsShouldProcess = true)]
[Alias("Delete-SonarrEpisodeFile")]
[MetadataCanPipe(Tag = Meta.Calendar)]
[MetadataCanPipe(Tag = Meta.Episode)]
[MetadataCanPipe(Tag = Meta.EpisodeFile)]
public sealed class RemoveSonarrEpisodeFileCmdlet : SonarrMetadataCmdlet
{
	SortedSet<int> _ids = null!;

	/// <summary>
	/// Gets or sets the IDs of the episode files to delete.
	/// </summary>
	/// <remarks>
	/// The IDs are those of the files, not of their episodes.
	/// </remarks>
	/// <value>The episode files' IDs, each greater than zero. The default is an empty array.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByEpisodeId")]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the objects whose episode files the cmdlet deletes.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, and each piped object arrives in an array of its own. It takes any object
	/// that implements <see cref="IEpisodeFilePipeable"/>, such as an episode, an episode file, or a calendar entry.
	/// An object's file ID can be 0, for an episode without a file, which the cmdlet skips with a warning, but not
	/// negative.
	/// </remarks>
	/// <value>The piped objects. The default is an empty array.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipelineInput")]
	[ValidateIds<IEpisodeFilePipeable>(ValidateRangeKind.NonNegative)]
	public IEpisodeFilePipeable[] InputObject { get; set; } = [];

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet deletes the files without asking first.
	/// </summary>
	/// <remarks>
	/// The switch skips both confirmation prompts. With <c>-Confirm</c>, the cmdlet still prompts through
	/// <c>ShouldProcess</c>, and in What if mode it still deletes nothing.
	/// </remarks>
	/// <value>The switch, which is present to skip the confirmation prompts.</value>
	[Parameter]
	public SwitchParameter Force { get; set; }

	/// <summary>
	/// Gets the metadata tag of the episode file endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The episode file's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.EpisodeFile];
	}
	/// <summary>
	/// Runs the base class's setup for the scope, then creates the set of file IDs that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
	}

	/// <summary>
	/// Adds the IDs from <c>-Id</c> to the files that the cmdlet deletes.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}
	/// <summary>
	/// Adds the file IDs of the piped objects to the files that the cmdlet deletes, and writes a warning for each piped
	/// object without a file.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Process(IServiceProvider provider)
	{
		if (this.HasParameter(InputObject) && this.InputObject.Length > 0)
		{
			EpisodeFileInput.AddFileIds(this, this.InputObject, _ids);
		}
	}
	/// <summary>
	/// Sends a delete request for each confirmed file, in the order of the files' IDs.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		bool yesToAll = false;
		bool noToAll = false;

		bool force = this.Force.ToBool();

		foreach (int id in _ids)
		{
			string url = this.Tag.GetUrlForId(id);
			if (this.ShouldProcessWithForce(url, "Deleting Episode File", force)
				&&
				(force
				||
				this.ShouldContinue(
					query: "Are you sure you want to delete the episode file?",
					caption: $"Delete Episode file: {id}",
					yesToAll: ref yesToAll,
					noToAll: ref noToAll)))
			{
				this.DeleteEpisodeFile(url);
			}
		}
	}

	/// <summary>
	/// Sends the delete request for one episode file, and writes a warning or an error when it fails.
	/// </summary>
	/// <remarks>
	/// An ignorable error, as Sonarr's answer for a file that it doesn't have is, becomes a warning, and any other
	/// error is written to the error stream.
	/// </remarks>
	/// <param name="url">The episode file's endpoint path.</param>
	private void DeleteEpisodeFile(string url)
	{
		var response = this.SendDeleteRequest(url);
		if (response.IsError)
		{
			if (response.Error.IsIgnorable)
			{
				this.WriteWarning(response.Error.Message);
			}
			else
			{
				this.WriteError(response.Error);
			}
		}
	}
}
