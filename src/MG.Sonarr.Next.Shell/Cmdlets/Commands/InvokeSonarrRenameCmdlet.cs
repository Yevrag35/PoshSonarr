using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Renames;
using MG.Sonarr.Next.Services.Jobs;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Commands;

/// <summary>
/// Represents the <c>Invoke-SonarrRename</c> cmdlet, which starts Sonarr's command that renames episode files to its
/// naming format.
/// </summary>
/// <remarks>
/// <para>
/// With <c>-SeriesId</c> and <c>-EpisodeFileId</c>, the cmdlet sends one rename command for those files of that
/// series. With piped input, such as the results of <c>Test-SonarrRename</c>, episodes, episode files, and calendar
/// entries, taken through <c>-InputObject</c> as <see cref="IRenameFilePipeable"/> values, it collects the files until
/// the pipeline ends and sends one command for each series, once that series' rename is confirmed. A file piped more
/// than once is renamed once.
/// </para>
/// <para>
/// The cmdlet writes each command that Sonarr queued to the pipeline as a
/// <see cref="MG.Sonarr.Next.Models.Commands.CommandObject"/>, which the
/// command history also keeps, and writes the error of a failed request, unless it's ignorable. Its requests write no
/// verbose or debug output.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Rename-SonarrEpisodeFile</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsLifecycle.Invoke, "SonarrRename", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
	DefaultParameterSetName = PSConstants.PSetExplicitId)]
[Alias("Rename-SonarrEpisodeFile")]
[MetadataCanPipe(Tag = Meta.Calendar)]
[MetadataCanPipe(Tag = Meta.Episode)]
[MetadataCanPipe(Tag = Meta.EpisodeFile)]
[MetadataCanPipe(Tag = Meta.Renamable)]
public sealed class InvokeSonarrRenameCmdlet : SonarrCmdletBase
{
	List<IRenameFilePipeable> _renameables = null!;

	/// <summary>
	/// Gets or sets the episode files to rename.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input. Each object has to implement <see cref="IRenameFilePipeable"/> and have an
	/// episode file ID greater than zero.
	/// </remarks>
	/// <value>The episode files. The default is an empty array.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	[ValidateIds<IRenameFilePipeable>(ValidateRangeKind.Positive)]
	[ValidateNotNull]
	public IRenameFilePipeable[] InputObject { get; set; } = [];

	/// <summary>
	/// Gets or sets the ID of the series whose episode files are renamed.
	/// </summary>
	/// <remarks>
	/// The ID has to be greater than zero.
	/// </remarks>
	/// <value>The series' ID.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int SeriesId { get; set; }

	/// <summary>
	/// Gets or sets the IDs of the episode files to rename.
	/// </summary>
	/// <remarks>
	/// Each ID has to be greater than zero, and the files belong to the series that <c>-SeriesId</c> names.
	/// </remarks>
	/// <value>The episode file IDs. The default is an empty array.</value>
	[Parameter(Mandatory = true, Position = 1, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] EpisodeFileId { get; set; } = [];

	/// <summary>
	/// Collects the piped episode files, which the cmdlet renames when the pipeline ends.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		if (PSConstants.PSetPipeline == this.ParameterSetName)
		{
			_renameables ??= new(this.InputObject.Length);
			_renameables.AddRange(this.InputObject);
		}
	}

	/// <summary>
	/// Sends the rename commands: one for each series of the piped files, or one for <c>-SeriesId</c> and
	/// <c>-EpisodeFileId</c>.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		var client = provider.GetRequiredService<ICommandTracker>();

		if (!_renameables.IsNullOrEmpty)
		{
			this.SendRenamesFromPipeline(client);
		}
		else
		{
			this.SendRenameFromExplicit(client);
		}
	}

	/// <summary>
	/// Sends the rename command for <c>-SeriesId</c> and <c>-EpisodeFileId</c> and writes the command that Sonarr
	/// queued.
	/// </summary>
	/// <remarks>
	/// When the request fails, the method writes its error, unless it's ignorable.
	/// </remarks>
	/// <param name="client">The tracker that sends the command and adds it to the command history.</param>
	private void SendRenameFromExplicit(ICommandTracker client)
	{
		PostRename rename = new()
		{
			FileIds = this.EpisodeFileId,
			SeriesId = this.SeriesId,
		};

		var response = client.SendRename(rename);
		_ = this.TryWriteObject(response);
	}
	/// <summary>
	/// Groups the piped episode files by series and sends a rename command for each series whose rename is confirmed,
	/// writing each command that Sonarr queued.
	/// </summary>
	/// <remarks>
	/// The confirmation names the series ID and the file IDs. When a request fails, the method writes its error, unless
	/// it's ignorable, and goes on to the next series.
	/// </remarks>
	/// <param name="client">The tracker that sends the commands and adds them to the command history.</param>
	private void SendRenamesFromPipeline(ICommandTracker client)
	{
		IEnumerable<PostRename> renames = PostRename.FromRenameObjects(_renameables);

		foreach (PostRename rename in renames)
		{
			if (this.ShouldProcess(rename.ToString(), "Rename Episode Files"))
			{
				var response = client.SendRename(rename);
				_ = this.TryWriteObject(response);
			}
		}
	}
}
