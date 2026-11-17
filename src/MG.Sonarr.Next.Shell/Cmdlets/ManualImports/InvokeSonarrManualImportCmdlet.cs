using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Exceptions.Shell;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.ManualImports;

/// <summary>
/// Represents the <c>Invoke-SonarrManualImport</c> cmdlet, which starts Sonarr's command that imports the files that
/// <c>Get-SonarrManualImport</c> found.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes each <see cref="ManualImportObject"/> piped to <c>-InputObject</c>, and confirms the import of
/// each one that is ready, naming its series and episode. A file that lacks an episode, a series, or a quality, which
/// <c>Edit-SonarrManualImport</c> sets, gets an error, and the cmdlet goes on to the next file.
/// </para>
/// <para>
/// When the pipeline ends, the cmdlet posts one <c>ManualImport</c> command with every confirmed file, which copies or
/// moves them as <c>-ImportMode</c> says. It writes nothing to the pipeline, and when the request fails, it writes the
/// error, unless it's ignorable. With <c>-Debug</c>, it writes the request's body to the debug stream itself.
/// </para>
/// </remarks>
[Cmdlet(VerbsLifecycle.Invoke, "SonarrManualImport", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
[MetadataCanPipe(Tag = Meta.ManualImport)]
public class InvokeSonarrManualImportCmdlet : SonarrMetadataCmdlet
{
	/// <summary>
	/// Gets a value that indicates whether the request's JSON body is written to the debug stream before the request is
	/// sent.
	/// </summary>
	/// <remarks>
	/// The cmdlet writes the body to the debug stream itself before it sends the request, so the HTTP handlers don't
	/// write it again.
	/// </remarks>
	/// <value>Always <see langword="false"/>.</value>
	public override bool CanDebugSerializeBefore => false;

	/// <summary>
	/// Gets or sets the files to import.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, and an empty array is allowed, which imports nothing.
	/// </remarks>
	/// <value>The files that Sonarr found for a manual import. The default is an empty array.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true), AllowEmptyCollection]
	public ManualImportObject[] InputObject { get; set; } = [];

	/// <summary>
	/// Gets or sets how Sonarr brings the files into the library, by copying or moving them.
	/// </summary>
	/// <value>The import mode. The default is <see cref="ManualImportMode.Copy"/>.</value>
	[Parameter]
	public ManualImportMode ImportMode { get; set; } = ManualImportMode.Copy;

	private MetadataList<ManualImportObject> _list = null!;
	/// <summary>
	/// Gets the metadata tag of the command endpoint, which the import is posted to.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The command endpoint's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.Command];
	}

	/// <summary>
	/// Adds each piped file that is ready to post, once its import is confirmed, to the files that the cmdlet imports
	/// when the pipeline ends.
	/// </summary>
	/// <remarks>
	/// A file that isn't ready gets an error record, with the file as its target, and the cmdlet goes on to the next
	/// file.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
	protected override void Process(IServiceProvider provider)
	{
		if (this.InputObject.Length == 0)
			return;

		foreach (ManualImportObject obj in this.InputObject)
		{
			if (!obj.IsReadyToPost())
			{
				this.WriteError(new ErrorRecord(new SonarrParameterException(nameof(InputObject), ParameterErrorType.Malformed, "The import is not ready to post as it's missing properties."),
					"InvokeSonarrManualImport:ImportNotReady", ErrorCategory.InvalidArgument, obj));

				continue;
			}

			if (this.ShouldProcess(this.Tag.UrlBase, $"Manual Import to Series '{obj.Series?.Title}' as episode number {obj.Episodes?.FirstOrDefault()?.AbsoluteEpisodeNumber}"))
			{
				_list ??= new(1);
				_list.Add(obj);
			}
		}
	}

	/// <summary>
	/// Posts one <c>ManualImport</c> command with every confirmed file, after writing its body to the debug stream.
	/// </summary>
	/// <remarks>
	/// When no file was confirmed, the cmdlet sends nothing. When the request fails, it writes the error, unless it's
	/// ignorable.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		if (_list is null or { Count: 0 })
			return;

		var body = new
		{
			Files = _list.ToManualImportFiles(),
			ImportMode = GetImportModeString(this.ImportMode),
			Name = "ManualImport",
		};

		this.SerializeIfDebug(body, includeType: false);

		SonarrClientResult response = this.SendPostRequest(this.Tag.UrlBase, body);
		if (response.IsError)
		{
			this.WriteConditionalError(response.Error);
		}
	}

	/// <summary>
	/// Clears and drops the list of confirmed files, then runs the base class's cleanup.
	/// </summary>
	/// <param name="disposing">
	/// <see langword="true"/> when the cmdlet is disposed; otherwise, <see langword="false"/>.
	/// </param>
	protected override void Dispose(bool disposing)
	{
		_list?.Clear();
		_list = null!;
		base.Dispose(disposing);
	}

	/// <summary>
	/// Returns the text that Sonarr reads for the specified import mode.
	/// </summary>
	/// <param name="mode">The import mode.</param>
	/// <returns><c>copy</c>, <c>move</c>, or an empty string for a value that the enum doesn't define.</returns>
	private static string GetImportModeString(ManualImportMode mode)
	{
		return mode switch
		{
			ManualImportMode.Copy => "copy",
			ManualImportMode.Move => "move",
			_ => string.Empty,
		};
	}
}

/// <summary>
/// Specifies how <c>Invoke-SonarrManualImport</c> brings files into the library.
/// </summary>
public enum ManualImportMode
{
	/// <summary>
	/// Sonarr copies the files and leaves the originals in place.
	/// </summary>
	Copy,
	/// <summary>
	/// Sonarr moves the files.
	/// </summary>
	Move,
}