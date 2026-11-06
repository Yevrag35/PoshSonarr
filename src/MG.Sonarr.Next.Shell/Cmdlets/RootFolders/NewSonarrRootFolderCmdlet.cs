using MG.Sonarr.Next.Exceptions.Shell;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RootFolders;

namespace MG.Sonarr.Next.Shell.Cmdlets.RootFolders;

/// <summary>
/// Represents the <c>New-SonarrRootFolder</c> cmdlet, which adds a root folder to Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet converts <c>-Path</c> to a full path from the current location, posts it to Sonarr, and writes the root
/// folder that Sonarr returns to the pipeline. <c>-Path</c> binds by property name, and through its <c>FullName</c>
/// alias, so a folder from <c>Get-Item</c> or <c>Get-ChildItem</c> can be piped to the cmdlet.
/// </para>
/// <para>
/// The cmdlet confirms each folder before it adds it, and its confirm impact is Low, so it prompts only under
/// <c>-Confirm</c> or a <c>$ConfirmPreference</c> of Low.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.New, "SonarrRootFolder", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
public sealed class NewSonarrRootFolderCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets or sets the metadata tag of the root folder endpoint, which the new folder is posted to.
	/// </summary>
	/// <value>The root folder's metadata tag, which <see cref="OnCreatingScope(IServiceProvider)"/> sets.</value>
	private MetadataTag Tag { get; set; } = null!;

	/// <summary>
	/// Gets or sets the path of the folder to add.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A relative path is converted to a full path from the current location, and the folder doesn't have to exist on
	/// this computer.
	/// </para>
	/// <para>
	/// The parameter binds by property name from the pipeline, and its alias is <c>FullName</c>.
	/// </para>
	/// </remarks>
	/// <value>The folder's path.</value>
	[Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
	[Alias("FullName")]
	public string Path { get; set; } = string.Empty;

	/// <summary>
	/// Runs the base class's setup for the scope, then gets the root folder's metadata tag from the scope.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		this.Tag = provider.GetRequiredService<IMetadataResolver>()[Meta.RootFolder];
	}
	/// <summary>
	/// Converts <c>-Path</c> to a full path and adds it to Sonarr as a root folder once the action is confirmed.
	/// </summary>
	/// <remarks>
	/// When the path can't be converted, the cmdlet writes the error and goes on to the next input.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		string path = this.GetAbsolutePath(this.Path);
		if (this.ShouldProcess(path, "Register Root Folder"))
		{
			this.RegisterFolder(path, s => new { Path = s });
		}
	}

	/// <summary>
	/// Converts the specified PowerShell path to a provider path from the current location, whether or not the item it
	/// names exists.
	/// </summary>
	/// <param name="providedPath">The path that <c>-Path</c> gives, which can be relative.</param>
	/// <returns>The provider path that <paramref name="providedPath"/> names.</returns>
	/// <exception cref="SonarrParameterException">
	/// Thrown when the path converts to an empty path or one of only white space.
	/// </exception>
	/// <exception cref="System.Management.Automation.DriveNotFoundException">
	/// Thrown when the path names a drive that doesn't exist.
	/// </exception>
	/// <exception cref="ProviderNotFoundException">
	/// Thrown when the path names a provider that isn't loaded.
	/// </exception>
	private string GetAbsolutePath(string providedPath)
	{
		string path = this.GetUnresolvedProviderPathFromPSPath(providedPath);
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new SonarrParameterException(nameof(this.Path), ParameterErrorType.Missing, null,
				new DirectoryNotFoundException($"Unable to resolve the path -> {providedPath}"));
		}

		return path;
	}
	/// <summary>
	/// Posts the body that the specified function builds from the specified path, and writes the root folder that
	/// Sonarr returns to the pipeline.
	/// </summary>
	/// <remarks>
	/// When the request fails, the method writes its error, whatever the error is.
	/// </remarks>
	/// <param name="path">The folder's full path.</param>
	/// <param name="toBody">The function that builds the request's body from <paramref name="path"/>.</param>
	private void RegisterFolder(string path, Func<string, object> toBody)
	{
		object obj = toBody(path);
		var response = this.SendPostRequest<RootFolderObject>(this.Tag.UrlBase, obj);
		if (response.TryGetT1(out var pso, out var error))
		{
			this.WriteObject(pso);
		}
		else
		{
			this.WriteError(error);
		}
	}
}
