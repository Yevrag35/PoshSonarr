using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Logs;

/// <summary>
/// Represents the <c>Get-SonarrLogFile</c> cmdlet, which gets the log files that Sonarr keeps.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet gets Sonarr's general log files from <c>/log/file</c> and its update log files from
/// <c>/log/file/update</c>, and writes them all to the pipeline as <see cref="LogFileObject"/> objects, sorted by
/// when they were last written. Each one can be piped to <c>Save-SonarrLogFile</c>.
/// </para>
/// <para>
/// When either request fails, the cmdlet writes its error and no log files.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrLogFile")]
public sealed class GetSonarrLogFileCmdlet : SonarrMetadataCmdlet
{
	/// <summary>
	/// Gets the metadata tag of the log file endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The log files' metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.LogFile];
	}

	/// <summary>
	/// Gets the general and the update log files and writes them to the pipeline, in one sorted list.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		var logs = this.GetAll<LogFileObject>();
		if (this.IsStopped)
		{
			return;
		}

		var updateLogs = this.SendGetRequest<MetadataList<LogFileObject>>(this.Tag.UrlBase + "/update");
		if (updateLogs.IsError)
		{
			this.WriteError(updateLogs.Error);
			return;
		}

		logs.AddRange(updateLogs.Value);
		logs.Sort();
		this.WriteCollection(logs);
	}
}
