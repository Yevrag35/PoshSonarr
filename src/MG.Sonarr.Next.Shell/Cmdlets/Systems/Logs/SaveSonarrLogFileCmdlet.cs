using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Shell.Attributes;
using System.Net;
using System.Text.Json;
using IOFile = System.IO.File;
using IOPath = System.IO.Path;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Logs;

/// <summary>
/// Represents the <c>Save-SonarrLogFile</c> cmdlet, which downloads Sonarr's log files to files.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes a log file's download URL from <c>-LogUri</c>, or log files from the pipeline or from
/// <c>-InputObject</c>, such as the objects that <c>Get-SonarrLogFile</c> writes. It downloads each log file from
/// Sonarr to <c>-Path</c>, and writes a <see cref="FileInfo"/> for each file that it writes.
/// </para>
/// <para>
/// <c>-Path</c> is a file with the <c>.txt</c> or <c>.log</c> extension, or a folder when it has no extension, in which
/// case each file takes the name of its log file. A path with any other extension ends the cmdlet with an error before
/// it downloads anything. A file that exists already isn't replaced, and the cmdlet writes an error for it and goes on
/// to the next log file, unless <c>-Force</c> is present.
/// </para>
/// <para>
/// A download that fails, as it does for a response that isn't a success, writes its error and leaves no file. When
/// the server's authentication needs credentials, <c>-Credential</c> supplies them.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Download-SonarrLogFile</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsData.Save, "SonarrLogFile", DefaultParameterSetName = "ByExplicitUrl")]
[Alias("Download-SonarrLogFile")]
[MetadataCanPipe(Tag = Meta.LogFile)]
[OutputType(typeof(FileInfo))]
public sealed class SaveSonarrLogFileCmdlet : SonarrCmdletBase, IApiCmdlet
{
	private bool _noFileName;
	/// <summary>
	/// Gets or sets the client that downloads the log files.
	/// </summary>
	/// <value>The download client of the cmdlet's scope.</value>
	private ISonarrDownloadClient Downloader { get; set; } = null!;
	/// <summary>
	/// Gets a value that indicates whether the cmdlet writes verbose messages about its requests.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if <see cref="SonarrCmdletBase.VerbosePreference"/> is neither
	/// <see cref="ActionPreference.SilentlyContinue"/> nor <see cref="ActionPreference.Ignore"/>; otherwise,
	/// <see langword="false"/>.
	/// </value>
	public bool CanWriteVerbose => this.VerbosePreference is not (ActionPreference.SilentlyContinue or ActionPreference.Ignore);

	/// <summary>
	/// Gets or sets the download URL of the log file to download, as Sonarr gives it.
	/// </summary>
	/// <remarks>
	/// The URL is relative, such as the <c>DownloadUrl</c> of a log file that <c>Get-SonarrLogFile</c> writes, which
	/// already starts with the instance's URL base. A URL that doesn't start with the base gets it in front. The
	/// parameter can't be combined with <c>-InputObject</c>.
	/// </remarks>
	/// <value>The log file's relative download URL.</value>
	[Parameter(Mandatory = true, ParameterSetName = "ByExplicitUrl")]
	[ValidateUrl(UriKind.Relative)]
	public string LogUri { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet replaces a file that exists already.
	/// </summary>
	/// <value>The switch, which is present to overwrite existing files.</value>
	[Parameter]
	public SwitchParameter Force { get; set; }

	/// <summary>
	/// Gets or sets the log file to download.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, such as the objects that <c>Get-SonarrLogFile</c> writes, and setting it sets
	/// <see cref="LogUri"/> to the log file's download URL. A read always gives <see langword="null"/>. The parameter
	/// can't be combined with <c>-LogUri</c>.
	/// </remarks>
	/// <value>Always <see langword="null"/> when read.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetPipeline, ValueFromPipeline = true)]
	public LogFileObject InputObject
	{
		get => null!;
		set => this.LogUri = value?.DownloadUrl ?? string.Empty;
	}

	/// <summary>
	/// Gets or sets the file or folder that the log files are saved to.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A relative path is resolved against the current location, and the path doesn't have to exist. A path with the
	/// <c>.txt</c> or <c>.log</c> extension is the file to write, and a path without an extension is a folder, which each
	/// log file is written to under its own file name. Any other extension ends the cmdlet with an error.
	/// </para>
	/// <para>
	/// When the cmdlet begins, it replaces the value with the resolved, absolute path.
	/// </para>
	/// </remarks>
	/// <value>The path of the file or folder.</value>
	[Parameter(Mandatory = true, Position = 0)]
	[ValidateNotNullOrEmpty]
	public string Path { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the credentials that the download sends when the server's authentication needs them.
	/// </summary>
	/// <remarks>
	/// The credentials go out only when the authentication type that the connection's test found needs them, as Basic
	/// or forms authentication does. A read always gives <see langword="null"/>.
	/// </remarks>
	/// <value>Always <see langword="null"/> when read.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[MaybeNull]
	[Parameter(Mandatory = false)]
	public PSCredential Credential
	{
		get => null;
		set => _creds = value?.GetNetworkCredential();
	}
	NetworkCredential? _creds;

	/// <summary>
	/// Gets a value that indicates whether the request's JSON body is written to the debug stream before the request is
	/// sent.
	/// </summary>
	/// <remarks>
	/// A download sends no body, so there is nothing to write.
	/// </remarks>
	/// <value>Always <see langword="false"/>.</value>
	public bool CanDebugSerializeBefore => false;
	/// <summary>
	/// Gets a value that indicates whether the response's body is written to the debug stream after the response is
	/// received.
	/// </summary>
	/// <remarks>
	/// The response's body is the log file, so the cmdlet never writes it to the debug stream.
	/// </remarks>
	/// <value>Always <see langword="false"/>.</value>
	public bool CanDebugSerializeAfter => false;

	/// <summary>
	/// Runs the base class's setup for the scope, then resolves the client that downloads the log files.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		this.Downloader = provider.GetRequiredService<ISonarrDownloadClient>();
	}

	/// <summary>
	/// Resolves <c>-Path</c> to an absolute path, and checks whether it names a <c>.txt</c> or <c>.log</c> file or a
	/// folder.
	/// </summary>
	/// <remarks>
	/// A path that can't be resolved, or one with an extension other than <c>.txt</c> or <c>.log</c>, ends the cmdlet
	/// with an error, so no log file is downloaded.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		ReadOnlySpan<char> originalPath = this.Path.AsSpan();
		this.Path = this.GetAbsolutePath(this.Path);
		if (!originalPath.Equals(this.Path, StringComparison.InvariantCulture))
		{
			this.WriteDebug($"{nameof(this.Path)} resolved to -> {this.Path}");
		}

		if (IsFileExtensionNotTxtOrLog(this.Path, out bool isEmpty))
		{
			this.StopCmdlet(
				new ArgumentException("If a target file extension is provided, it must be '.txt' or '.log'.")
					.ToRecord(ErrorCategory.InvalidArgument, this.Path));

			return;
		}
		else if (isEmpty)
		{
			this.WriteDebug($"No file name detected in provided path. Will add from {nameof(this.LogUri)} or incoming {nameof(this.InputObject)}");
			_noFileName = true;
		}
	}
	/// <summary>
	/// Downloads the log file to its file and writes a <see cref="FileInfo"/> for the file.
	/// </summary>
	/// <remarks>
	/// When the file exists already and <c>-Force</c> isn't present, the cmdlet writes an error and downloads nothing.
	/// When the download fails, the cmdlet writes its error.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		string downloadPath = this.MakeFilePath(this.Path, this.LogUri, _noFileName);
		if (!this.Force && IOFile.Exists(downloadPath))
		{
			this.WriteError(new IOException($"The file '{downloadPath}' already exists.")
				.ToRecord(ErrorCategory.WriteError, downloadPath));

			return;
		}

		this.WriteVerbose($"Writing response to -> {downloadPath}");
		var response = this.Downloader.DownloadToPathAsync(this.LogUri, downloadPath, _creds).GetAwaiter().GetResult();

		if (response.IsError)
		{
			this.WriteError(response.Error);
			return;
		}

		if (!string.IsNullOrWhiteSpace(response.Value))
		{
			FileInfo fi = new(downloadPath);
			this.WriteObject(fi);
		}
	}

	/// <summary>
	/// Resolves the specified PowerShell path to an absolute file system path, which doesn't have to exist.
	/// </summary>
	/// <param name="providedPath">The path that <c>-Path</c> gave, which can be relative to the current location.</param>
	/// <returns>The absolute path.</returns>
	/// <exception cref="ArgumentException">Thrown when the path resolves to an empty path.</exception>
	private string GetAbsolutePath(string providedPath)
	{
		string path = this.GetUnresolvedProviderPathFromPSPath(providedPath);
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("The provided path is invalid.", new DirectoryNotFoundException($"Unable to resolve the path -> {providedPath}"));
		}

		return path;
	}
	/// <summary>
	/// Determines whether the specified path has a file extension other than <c>.txt</c> or <c>.log</c>, and whether it
	/// has one at all.
	/// </summary>
	/// <remarks>
	/// The extension is compared ignoring case, so <c>.LOG</c> counts as <c>.log</c>.
	/// </remarks>
	/// <param name="path">The path to check.</param>
	/// <param name="isEmpty">
	/// When the method returns, <see langword="true"/> if the path has no extension, which makes it a folder; otherwise,
	/// <see langword="false"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the path has an extension other than <c>.txt</c> or <c>.log</c>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	private static bool IsFileExtensionNotTxtOrLog(ReadOnlySpan<char> path, out bool isEmpty)
	{
		ReadOnlySpan<char> fileExt = IOPath.GetExtension(path);
		isEmpty = fileExt.IsEmpty;
		return !isEmpty
			   &&
			   !fileExt.Equals(stackalloc char[] { '.', 't', 'x', 't' },
					StringComparison.InvariantCultureIgnoreCase)
			   &&
			   !fileExt.Equals(stackalloc char[] { '.', 'l', 'o', 'g' },
					StringComparison.InvariantCultureIgnoreCase);
	}
	/// <summary>
	/// Gets the path of the file that a log file is written to.
	/// </summary>
	/// <param name="dirPath">The absolute path from <c>-Path</c>, a file or a folder.</param>
	/// <param name="logUrl">The log file's download URL, whose last segment is its file name.</param>
	/// <param name="noFileExtension">
	/// <see langword="true"/> if <paramref name="dirPath"/> is a folder, to add the log file's name to it; otherwise,
	/// <see langword="false"/> to use <paramref name="dirPath"/> as the file.
	/// </param>
	/// <returns>The path of the file to write.</returns>
	private string MakeFilePath(string dirPath, string logUrl, bool noFileExtension)
	{
		if (!noFileExtension)
		{
			return dirPath;
		}

		string fileName = IOPath.GetFileName(logUrl);
		this.WriteDebug($"Appending file name to {nameof(this.Path)} -> {fileName}");
		return IOPath.Combine(dirPath, fileName);
	}
	/// <summary>
	/// Writes the body of a request or a response to the debug stream.
	/// </summary>
	/// <remarks>
	/// The text goes to the host's UI as a debug line, so it can be written from any thread. When the host has no UI,
	/// nothing is written. The method doesn't check <see cref="CanDebugSerializeBefore"/> or
	/// <see cref="CanDebugSerializeAfter"/>; the caller does.
	/// </remarks>
	/// <param name="jsonPayload">The text to write, which describes the body and holds its JSON.</param>
	public void WriteDebugPayload(string jsonPayload)
	{
		if (this.Host?.UI is not null)
		{
			this.Host.UI.WriteDebugLine(jsonPayload);
		}
	}
	/// <summary>
	/// Writes a verbose line about a request that is about to be sent, with its method and URL.
	/// </summary>
	/// <remarks>
	/// The line reads like <c>Sending GET request -&gt; http://host/logfile/sonarr.txt</c>. The method doesn't
	/// check <see cref="CanWriteVerbose"/>; the caller does.
	/// </remarks>
	/// <param name="request">The details of the pending request.</param>
	public void WriteVerboseBefore(IHttpRequestDetails request)
	{
		this.WriteVerbose($"Sending {request.RequestMethod} request -> {request.RequestUrl}");
	}
	/// <summary>
	/// Writes a verbose line that reports the response to a request, as the JSON of the request's outcome.
	/// </summary>
	/// <remarks>
	/// The line goes to the host's UI, so it can be written from any thread. When the host has no UI, nothing is
	/// written.
	/// </remarks>
	/// <param name="response">The outcome of the request, with the response's status code and URL.</param>
	/// <param name="provider">
	/// A scoped service provider, whose <see cref="ISonarrJsonOptions"/> serializes the outcome when
	/// <paramref name="options"/> is <see langword="null"/>.
	/// </param>
	/// <param name="options">
	/// The JSON options to serialize the outcome with, or <see langword="null"/> to use the serializing options of
	/// <paramref name="provider"/>.
	/// </param>
	public void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options = null)
	{
		if (this.VerbosePreference != ActionPreference.SilentlyContinue && this.Host?.UI is not null)
		{
			options ??= provider.GetService<ISonarrJsonOptions>()?.ForSerializing;
			this.Host.UI.WriteVerboseLine(JsonSerializer.Serialize(response, options));
		}
	}
}
