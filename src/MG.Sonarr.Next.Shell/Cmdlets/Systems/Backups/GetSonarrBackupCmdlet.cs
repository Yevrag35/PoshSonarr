using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Backups;

/// <summary>
/// Represents the <c>Get-SonarrBackup</c> cmdlet, which gets the backups that Sonarr keeps.
/// </summary>
/// <remarks>
/// The cmdlet gets every backup from <c>/system/backup</c> and writes each one to the pipeline as a
/// <see cref="BackupObject"/>, sorted by name and then ID. When the request fails, the cmdlet writes its error and
/// nothing else.
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrBackup")]
public sealed class GetSonarrBackupCmdlet : SonarrMetadataCmdlet
{
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
	/// Gets every backup and writes each one to the pipeline.
	/// </summary>
	/// <remarks>
	/// When the request fails, the cmdlet stops with its error.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		var backups = this.GetAll<BackupObject>();
		this.WriteCollection(backups);
	}
}
