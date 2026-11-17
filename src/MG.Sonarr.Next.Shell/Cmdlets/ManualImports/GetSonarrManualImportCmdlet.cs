using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Internal;
using System.Net;

//using CanBeNullAttribute = System.Management.Automation.AllowNullAttribute;

namespace MG.Sonarr.Next.Shell.Cmdlets.ManualImports;

/// <summary>
/// Represents the <c>Get-SonarrManualImport</c> cmdlet, which gets the files that Sonarr finds for a manual import.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet looks in a folder by <c>-Path</c>, in a download by <c>-DownloadId</c>, or in a series' folder by
/// <c>-SeriesId</c> or a <see cref="SeriesObject"/> piped to <c>-Series</c>, where <c>-SeasonNumber</c> limits the
/// files to one season. Sonarr returns the files that it would import, and the cmdlet writes each one to the pipeline
/// as a <see cref="ManualImportObject"/>, which <c>Edit-SonarrManualImport</c> and <c>Invoke-SonarrManualImport</c>
/// take from the pipeline. When the request fails, the cmdlet writes its error, unless it's ignorable.
/// </para>
/// <para>
/// Sonarr leaves out the files that are already in the library, unless <c>-IncludeExistingFiles</c> is given.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrManualImport", DefaultParameterSetName = BY_FOLDER)]
//[OutputType(typeof())]
public sealed class GetSonarrManualImportCmdlet : SonarrApiCmdletBase
{
	private const string BY_FOLDER = "ByFolderPath";
	private const string BY_DOWNLOAD = "ByDownloadId";
	private const string BY_SERIES = "ByExplicitSeriesId";
	private const string BY_SERIES_PIPE = "BySeriesPipelineInput";
	private const string FILTER_EXISTING = "filterExistingFiles";
	private const string FOLDER = "folder";
	private QueryCol _parameters = null!;

	/// <summary>
	/// Gets or sets the path of the folder that Sonarr looks for files in.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The parameter also binds from a piped object's <c>Path</c> or <c>PSPath</c> property, such as a folder from
	/// <c>Get-Item</c>, and its alias is <c>-PSPath</c>.
	/// </para>
	/// <para>
	/// A path that's fully qualified on Linux, macOS, or Windows is sent as it is, since Sonarr reads it on its own
	/// server: one that starts with <c>/</c>, as in <c>/downloads/tv</c>, a UNC path that starts with <c>\\</c> or
	/// <c>//</c>, or a drive path such as <c>C:\downloads\tv</c>. The cmdlet checks only the path's text, the same way
	/// on every platform, so a session on any platform can give a path for a Sonarr on any other.
	/// </para>
	/// <para>
	/// Any other path, such as a relative one or the <c>PSPath</c> of a folder from <c>Get-Item</c>, is resolved as a
	/// PowerShell path, against the current location, and has to exist.
	/// </para>
	/// <para>
	/// Either way, the cmdlet trims the trailing slashes from the path it sends, but not the slashes of a root, so
	/// <c>/downloads/</c> goes out as <c>/downloads</c>, while <c>/</c> and <c>C:\</c> go out as they are.
	/// </para>
	/// </remarks>
	/// <value>The folder's path. The default is an empty string.</value>
	[Parameter(
		Mandatory = true,
		Position = 0,
		ValueFromPipelineByPropertyName = true,
		ParameterSetName = BY_FOLDER)]
	[Alias("PSPath")]
	[ValidateNotNullOrEmpty]
	public string Path { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the ID of the download whose files Sonarr finds.
	/// </summary>
	/// <remarks>
	/// The ID is the one that the download client gave the download, and it can't be <see langword="null"/> or empty.
	/// </remarks>
	/// <value>The download's ID. The default is an empty string.</value>
	[Parameter(Mandatory = true, ParameterSetName = BY_DOWNLOAD)]
	[ValidateNotNullOrEmpty]
	public string DownloadId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the ID of the series whose folder Sonarr looks for files in.
	/// </summary>
	/// <remarks>
	/// The ID has to be greater than zero. A series piped to <c>-Series</c> sets it to the series' ID.
	/// </remarks>
	/// <value>The series' ID.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = BY_SERIES)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int SeriesId { get; set; }

	/// <summary>
	/// Gets or sets the series whose folder Sonarr looks for files in.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input. Setting it sets <see cref="SeriesId"/> to the series' ID, and the getter
	/// always returns <see langword="null"/>.
	/// </remarks>
	/// <value>Always <see langword="null"/> when read.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ParameterSetName = BY_SERIES_PIPE, ValueFromPipeline = true)]
	[ValidateNotNull]
	public SeriesObject Series
	{
		get => null!;
		set => this.SeriesId = value?.Id ?? 0;
	}

	/// <summary>
	/// Gets or sets the number of the season whose files Sonarr finds.
	/// </summary>
	/// <remarks>
	/// The parameter applies only to a series, and the number can't be negative.
	/// </remarks>
	/// <value>The season number. When the parameter isn't given, Sonarr finds the files of every season.</value>
	[Parameter(Position = 1, ParameterSetName = BY_SERIES)]
	[Parameter(Position = 0, ParameterSetName = BY_SERIES_PIPE)]
	[ValidateRange(ValidateRangeKind.NonNegative)]
	public int SeasonNumber { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether Sonarr also returns the files that are already in the library.
	/// </summary>
	/// <value>The switch, which is present to include existing files.</value>
	[Parameter]
	public SwitchParameter IncludeExistingFiles { get; set; }

	/// <summary>
	/// Runs the base class's setup for the scope, then creates the query that the cmdlet's requests send.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_parameters = new(10);
	}
	/// <summary>
	/// Adds to the query the download ID, or the series ID and season number, that the parameter set gives, and
	/// whether Sonarr leaves out existing files.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		switch (this.ParameterSetName)
		{
			case BY_DOWNLOAD:
				_parameters.Add(nameof(this.DownloadId), this.DownloadId);
				break;

			case BY_SERIES:
			case BY_SERIES_PIPE:
				SetSeriesParameters(
					this.SeriesId,
					this.SeasonNumber,
					wantsSeason: this.HasParameter(this.SeasonNumber),
					_parameters);

				break;

			default:
				break;
		}

		SetFilterParameter(this.IncludeExistingFiles, _parameters);
	}
	/// <summary>
	/// Gets the files that Sonarr finds and writes them to the pipeline.
	/// </summary>
	/// <remarks>
	/// For a folder, the cmdlet resolves the path and sets it in the query first, replacing the previous input's.
	/// When the request fails, the cmdlet writes its error, unless it's ignorable.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		var tag = provider.GetMetadataTag(Meta.ManualImport);

		if (this.ParameterSetName == BY_FOLDER)
		{
			string formattedPath = this.ResolveAndFormatPath(this.Path);
			_parameters.AddOrUpdate(FOLDER, formattedPath);
		}

		string url = tag.GetUrl(_parameters);
		var response = this.SendGetRequest<MetadataList<ManualImportObject>>(url);
		_ = this.TryWriteObject(response);
	}

	/// <summary>
	/// Resolves the specified path when it isn't fully qualified on Linux, macOS, or Windows, trims its trailing slashes
	/// down to its root, and URL-encodes it for the query.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A UNC path counts as fully qualified, and the check reads only the path's text, so a path on Sonarr's server is
	/// sent as it is, whatever platform the session runs on. The path is resolved before it's trimmed, since trimming
	/// a PowerShell path such as <c>Temp:\</c> would leave <c>Temp:</c>, which names the drive's current location
	/// rather than its root.
	/// </para>
	/// <para>
	/// The method writes the path to the verbose stream before and after encoding it.
	/// </para>
	/// </remarks>
	/// <param name="path">The folder's path, as <c>-Path</c> gave it.</param>
	/// <returns>The URL-encoded path.</returns>
	/// <exception cref="ItemNotFoundException">
	/// Thrown when the path isn't fully qualified and names an item that doesn't exist.
	/// </exception>
	private string ResolveAndFormatPath(string path)
	{
		if (!ServerPath.IsFullyQualified(path, allowUnc: true))
		{
			path = this.GetResolvedPath(path);
		}

		path = ServerPath.TrimEndSeparators(path);

		this.WriteVerbose($"Original path -> {path}");
		path = WebUtility.UrlEncode(path);
		this.WriteVerbose($"URL-formatted path -> {path}");

		return path;
	}
	/// <summary>
	/// Adds the query parameter that tells Sonarr whether to leave out the files that are already in the library.
	/// </summary>
	/// <param name="includeExisting">
	/// <see langword="true"/> to include existing files; otherwise, <see langword="false"/>.
	/// </param>
	/// <param name="parameters">The query to add to.</param>
	private static void SetFilterParameter(bool includeExisting, QueryCol parameters)
	{
		parameters.Add([FILTER_EXISTING, !includeExisting]);
	}
	/// <summary>
	/// Adds the series ID, and the season number when it's wanted, to the specified query.
	/// </summary>
	/// <remarks>
	/// The query's keys are the names of the parameters, <c>seriesId</c> and <c>seasonNumber</c>.
	/// </remarks>
	/// <param name="seriesId">The series' ID.</param>
	/// <param name="seasonNumber">The season number.</param>
	/// <param name="wantsSeason">
	/// <see langword="true"/> to add the season number; otherwise, <see langword="false"/>.
	/// </param>
	/// <param name="parameters">The query to add to.</param>
	private static void SetSeriesParameters(int seriesId, int seasonNumber, bool wantsSeason, QueryCol parameters)
	{
		parameters.Add(nameof(seriesId), seriesId);
		if (wantsSeason)
		{
			parameters.Add(nameof(seasonNumber), seasonNumber);
		}
	}
}

