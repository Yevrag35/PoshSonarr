using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Exceptions.Shell;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RemotePaths;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Internal;

namespace MG.Sonarr.Next.Shell.Cmdlets.RemotePath;

/// <summary>
/// Represents the <c>New-SonarrRemotePathMapping</c> cmdlet, which creates a remote path mapping in Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// A mapping tells Sonarr that a path that the download client on <c>-HostName</c> reports, <c>-RemotePath</c>, is
/// the path <c>-LocalPath</c> where Sonarr runs. The cmdlet posts the mapping, and writes the new mapping that Sonarr
/// returns to the pipeline as a <see cref="RemotePathObject"/>.
/// </para>
/// <para>
/// When <c>-HostName</c> is <c>localhost</c> or <c>.</c>, the cmdlet sends this computer's name in its place, and
/// <c>-LocalPath</c> converted to a full path from the current location, whether or not the item it names exists.
/// For any other host, <c>-LocalPath</c> has to be an absolute path on the computer where Sonarr runs, in the form
/// of Linux, macOS, or Windows, and the cmdlet sends it as it's given. A <c>-LocalPath</c> that doesn't suit the host
/// stops the cmdlet with an error, so no mapping is created.
/// </para>
/// <para>
/// The cmdlet confirms the mapping before it creates it, and its confirm impact is Low, so it prompts only under
/// <c>-Confirm</c> or a <c>$ConfirmPreference</c> of Low.
/// </para>
/// <para>
/// The cmdlet's aliases are <c>New-SonarrRemotePath</c> and <c>New-SonarrRemoteMapping</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.New, "SonarrRemotePathMapping", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
[Alias("New-SonarrRemotePath", "New-SonarrRemoteMapping")]
public sealed class NewSonarrRemotePathMappingCmdlet : SonarrMetadataCmdlet
{
	private const string USERDNSDOMAIN = "USERDNSDOMAIN";
	private const string LOCALHOST = "localhost";
	private const string DOT_HOST = ".";

	/// <summary>
	/// Gets a value that indicates whether the cmdlet captures its debug preference before processing begins.
	/// </summary>
	/// <remarks>
	/// The cmdlet writes its request body through <see cref="SonarrCmdletBase.SerializeIfDebug"/>, which writes only
	/// when the captured preference that <c>-Debug</c> or <c>$DebugPreference</c> gives allows it.
	/// </remarks>
	/// <value>Always <see langword="true"/>.</value>
	protected override bool CaptureDebugPreference => true;
	/// <summary>
	/// Gets a value that indicates whether the request's JSON body is written to the debug stream before the request is
	/// sent.
	/// </summary>
	/// <remarks>
	/// The cmdlet writes the body to the debug stream itself, through <see cref="SonarrCmdletBase.SerializeIfDebug"/>,
	/// so the property returns <see langword="false"/> and the body appears once.
	/// </remarks>
	/// <value>Always <see langword="false"/>.</value>
	public override bool CanDebugSerializeBefore => false;

	/// <summary>
	/// Gets or sets the host of the download client that the mapping applies to.
	/// </summary>
	/// <remarks>
	/// <c>localhost</c>, in any case, and <c>.</c> stand for this computer: the cmdlet replaces either with the
	/// computer's name, followed by a <c>.</c> and the <c>USERDNSDOMAIN</c> environment variable in lowercase when
	/// that variable is set.
	/// </remarks>
	/// <value>The host's name, which can't be empty.</value>
	[Parameter(Mandatory = true)]
	[ValidateNotNullOrEmpty]
	public string HostName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the path that the download client reports.
	/// </summary>
	/// <value>The download client's path, which can't be empty.</value>
	[Parameter(Mandatory = true)]
	[ValidateNotNullOrEmpty]
	public string RemotePath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the path that Sonarr uses in place of the remote path.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The path can't be empty, and it doesn't have to exist. Which paths are allowed depends on <c>-HostName</c>, so
	/// the cmdlet checks the path once its parameters are bound, and a path that isn't allowed stops the cmdlet with an
	/// error, so no mapping is created.
	/// </para>
	/// <para>
	/// When <c>-HostName</c> stands for this computer, the path can be relative, and the cmdlet sends the full path that
	/// it converts to from the current location, without expanding wildcards in it. That full path has to be a local
	/// path on this computer, not a UNC path.
	/// </para>
	/// <para>
	/// For any other host, the path has to be absolute on the computer where Sonarr runs, so it has to start with a
	/// single <c>/</c>, as in <c>/data/tv</c>, or with a drive letter, a colon, and a separator, as in <c>C:\tv</c>. The
	/// cmdlet checks only the path's text, the same way on every platform, so a session on any platform can give a
	/// path for a Sonarr on any other. Any other path, such as <c>.\tv</c>, <c>tv</c>, <c>~/tv</c>, <c>C:tv</c>, or the
	/// UNC paths <c>\\server\tv</c> and <c>//server/tv</c>, isn't allowed.
	/// </para>
	/// </remarks>
	/// <value>The local path.</value>
	[Parameter(Mandatory = true)]
	[ValidateNotNullOrEmpty]
	public string LocalPath { get; set; } = string.Empty;

	/// <summary>
	/// Gets the metadata tag of the remote path mapping endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The remote path mapping's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.RemotePathMapping];
	}

	/// <summary>
	/// Checks <c>-LocalPath</c> for the host that <c>-HostName</c> names, and when that's <c>localhost</c> or <c>.</c>,
	/// replaces <c>-HostName</c> with this computer's name and <c>-LocalPath</c> with its full path.
	/// </summary>
	/// <remarks>
	/// <para>
	/// For this computer, the path is converted from the current location, so the item it names doesn't have to exist,
	/// and wildcards in it aren't expanded. When the conversion throws, as for a drive that doesn't exist, the cmdlet
	/// ends with the error, and a full path that isn't a local, non-UNC path stops the cmdlet with an error.
	/// </para>
	/// <para>
	/// For any other host, the path is one on the computer where Sonarr runs, which neither the current location nor
	/// this computer's drives say anything about, so the cmdlet checks only its text, and a path that isn't absolute
	/// there, or is a UNC path, stops the cmdlet with an error.
	/// </para>
	/// <para>
	/// Either error's target is the path as <c>-LocalPath</c> gave it, and after it,
	/// <see cref="Process(IServiceProvider)"/> doesn't run.
	/// </para>
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		if (LOCALHOST.Equals(this.HostName, StringComparison.OrdinalIgnoreCase) || DOT_HOST.Equals(this.HostName, StringComparison.Ordinal))
		{
			string path = this.GetUnresolvedPath(this.LocalPath);
			if (!IsLocalPathOnThisComputer(path))
			{
				this.StopForInvalidLocalPath(
					$"The path '{path}' isn't a local path on this computer. Give a file system path that isn't a UNC path.");
				return;
			}

			this.LocalPath = path;
			this.HostName = GetLocalHostName(Environment.MachineName);
		}
		else if (!ServerPath.IsFullyQualified(this.LocalPath, allowUnc: false))
		{
			this.StopForInvalidLocalPath(
				$"With the host '{this.HostName}', the path has to be absolute on the computer where Sonarr runs, such as '/data/tv' or 'C:\\tv', and not a UNC path.");
		}
	}
	/// <summary>
	/// Builds the body of the new remote path mapping and posts it to Sonarr once the action is confirmed.
	/// </summary>
	/// <remarks>
	/// The body's JSON goes to the debug stream before the cmdlet confirms the request. The body is a
	/// <see cref="RemotePathBody"/>, which serializing leaves unchanged, so writing it first changes nothing, under
	/// <c>-WhatIf</c> either.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		RemotePathBody newMap = new()
		{
			Host = this.HostName,
			RemotePath = this.RemotePath,
			LocalPath = this.LocalPath,
		};

		this.SerializeIfDebug(newMap, includeType: false);
		if (!this.ShouldProcess(this.Tag.UrlBase, "Creating Remote Path Mapping"))
		{
			return;
		}

		this.SendNewRequest(newMap, this.Tag);
	}

	/// <summary>
	/// Determines whether the specified full path is a local path on this computer, one that isn't a UNC path.
	/// </summary>
	/// <remarks>
	/// A path that a provider other than the file system's gives, such as a registry key's, isn't an absolute URI, so
	/// it isn't one.
	/// </remarks>
	/// <param name="path">The full path that a provider converted <c>-LocalPath</c> to.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="path"/> is an absolute path and not a UNC path; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	private static bool IsLocalPathOnThisComputer(string path)
	{
		return Uri.TryCreate(path, UriKind.Absolute, out Uri? uri) && !uri.IsUnc;
	}
	/// <summary>
	/// Stops the cmdlet with an error that says <c>-LocalPath</c> isn't allowed.
	/// </summary>
	/// <remarks>
	/// The error's category is <see cref="ErrorCategory.InvalidArgument"/>, and its target is the path as
	/// <c>-LocalPath</c> gave it.
	/// </remarks>
	/// <param name="message">The text that says what's wrong with the path and which path to give instead.</param>
	private void StopForInvalidLocalPath(string message)
	{
		this.StopCmdlet(new SonarrErrorRecord(
			normalEx: new SonarrParameterException(nameof(this.LocalPath), ParameterErrorType.Invalid, message),
			"New-SonarrRemotePathMapping.InvalidLocalPath",
			ErrorCategory.InvalidArgument,
			this.LocalPath));
	}
	/// <summary>
	/// Gets the specified computer name, followed by a <c>.</c> and the value of the <c>USERDNSDOMAIN</c> environment
	/// variable in lowercase, or the name alone when the variable isn't set or is only white space.
	/// </summary>
	/// <remarks>
	/// The domain is lowercased with the invariant culture, into the new string's own buffer.
	/// </remarks>
	/// <param name="localHostName">The computer's name, from <see cref="Environment.MachineName"/>.</param>
	/// <returns>The computer's name, with its DNS domain when there is one.</returns>
	private static string GetLocalHostName(string localHostName)
	{
		string? domain = Environment.GetEnvironmentVariable(USERDNSDOMAIN);
		if (string.IsNullOrWhiteSpace(domain))
		{
			return localHostName;
		}

		int length = localHostName.Length + domain.Length + 1;
		return string.Create(length, (localHostName, domain), (chars, state) =>
		{
			state.localHostName.CopyTo(chars);
			int position = state.localHostName.Length;

			chars[position++] = '.';

			scoped Span<char> domain = chars.Slice(position);
			state.domain.AsSpan().ToLower(domain, CultureInfo.InvariantCulture);
		});
	}
	/// <summary>
	/// Posts the specified body to the endpoint of the specified tag, and writes the new remote path mapping that
	/// Sonarr returns to the pipeline.
	/// </summary>
	/// <remarks>
	/// When the request fails, the method writes its error, whatever the error is.
	/// </remarks>
	/// <param name="body">The new mapping's body.</param>
	/// <param name="tag">The metadata tag of the remote path mapping endpoint.</param>
	private void SendNewRequest(RemotePathBody body, MetadataTag tag)
	{
		var oneOf = this.SendPostRequest<RemotePathObject>(tag.UrlBase, body);
		if (oneOf.TryGetT2(out var error, out var remotePath))
		{
			this.WriteError(error);
		}
		else
		{
			this.WriteObject(remotePath);
		}
	}
}
