using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Backups;

/// <summary>
/// Represents the <c>Remove-SonarrBackup</c> cmdlet, which deletes backups from Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes the backups' IDs from <c>-Id</c>, or the backups themselves from the pipeline or from
/// <c>-InputObject</c>, such as the objects that <c>Get-SonarrBackup</c> writes. It collects the IDs of every input,
/// and once the input ends, sends a <c>DELETE</c> to <c>/system/backup/{id}</c> for each ID, in ascending order and
/// once for an ID given more than once. It writes nothing to the pipeline.
/// </para>
/// <para>
/// Before each deletion, the cmdlet asks for confirmation as <c>ShouldProcess</c> does, and then asks again whether to
/// delete the backup, with answers of Yes to All and No to All that hold for the rest of the backups. <c>-Force</c>
/// skips both questions, unless <c>-Confirm</c> is given, which still asks the first, and in What if mode the cmdlet
/// shows its What if line for each backup and deletes nothing.
/// </para>
/// <para>
/// A deletion that fails with an ignorable error, such as the 404 for a backup that doesn't exist, writes the error's
/// message as a warning, and any other failure writes its error and goes on to the next backup.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Delete-SonarrBackup</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Remove, "SonarrBackup", SupportsShouldProcess = true,
	DefaultParameterSetName = PSConstants.PSetExplicitId)]
[Alias("Delete-SonarrBackup")]
[MetadataCanPipe(Tag = Meta.Backup)]
public sealed class RemoveSonarrBackupCmdlet : SonarrMetadataCmdlet
{
	SortedSet<int> _ids = null!;
	bool _yesToAll;
	bool _noToAll;

	/// <summary>
	/// Gets or sets the IDs of the backups to delete.
	/// </summary>
	/// <remarks>
	/// Each ID has to be greater than zero. The parameter can't be combined with <c>-InputObject</c>.
	/// </remarks>
	/// <value>The backups' IDs.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the backups to delete.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, such as the objects that <c>Get-SonarrBackup</c> writes. Each backup's ID has
	/// to be greater than zero. The parameter can't be combined with <c>-Id</c>.
	/// </remarks>
	/// <value>The backups to delete.</value>
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetPipeline, ValueFromPipeline = true)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public BackupObject[] InputObject { get; set; } = [];

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet deletes the backups without asking for confirmation.
	/// </summary>
	/// <remarks>
	/// The switch skips both of the cmdlet's questions, unless <c>-Confirm</c> is given, which still asks the first. In
	/// What if mode, the cmdlet deletes nothing, whether the switch is present or not.
	/// </remarks>
	/// <value>The switch, which is present to skip the confirmation.</value>
	[Parameter]
	public SwitchParameter Force { get; set; }

	/// <summary>
	/// Gets the metadata tag of the backup endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The backups' metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.Backup];
	}
	/// <summary>
	/// Runs the base class's setup for the scope, then creates the set that collects the IDs of the backups to delete.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
	}

	/// <summary>
	/// Adds the IDs from <c>-Id</c>, and those of the backups in <c>-InputObject</c>, to the set of IDs to delete.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
		if (this.HasParameter(this.InputObject))
		{
			_ids.UnionWith(this.InputObject.Select(x => x.Id));
		}
	}

	/// <summary>
	/// Deletes each collected backup that the user confirms, in ascending order of ID.
	/// </summary>
	/// <remarks>
	/// The What if line and the first question name the backup's path, and the second question names its ID.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		bool force = this.Force.ToBool();
		foreach (int id in _ids)
		{
			string url = this.Tag.GetUrlForId(id);
			if (this.ShouldProcessWithForce(url, "Deleting Backup", force)
				&&
				(force
				||
				this.ShouldContinue(query: "Are you sure you want to delete the backup?",
					caption: $"Delete Backup: {id}",
					yesToAll: ref _yesToAll,
					noToAll: ref _noToAll)))
			{
				this.DeleteBackup(url);
			}
		}
	}

	/// <summary>
	/// Sends a <c>DELETE</c> for the backup at the specified path, and reports a failure as a warning or an error.
	/// </summary>
	/// <remarks>
	/// An ignorable error, such as the 404 for a backup that doesn't exist, becomes a warning with the error's message,
	/// and any other error is written to the error stream.
	/// </remarks>
	/// <param name="url">The backup's path, relative to the API, such as <c>/system/backup/3</c>.</param>
	private void DeleteBackup(string url)
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
