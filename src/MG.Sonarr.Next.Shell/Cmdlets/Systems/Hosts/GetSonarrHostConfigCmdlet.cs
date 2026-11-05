using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Services.Http;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Hosts;

/// <summary>
/// Represents the <c>Get-SonarrHostConfig</c> cmdlet, which gets Sonarr's host settings.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet gets the host settings from <c>/config/host</c>, such as the port, the URL base, authentication, and the
/// proxy, and writes them to the pipeline. By default it writes a <see cref="NoKeyHostObject"/>, which hides the API
/// key and the passwords, and <c>-IncludeSensitiveInfo</c> writes a <see cref="HostObject"/> that shows them. Either
/// one can be changed and piped to <c>Update-SonarrHostConfig</c>.
/// </para>
/// <para>
/// When the request fails, the cmdlet writes its error, unless the error is ignorable.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Get-SonarrHost</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrHostConfig")]
[Alias("Get-SonarrHost")]
public sealed class GetSonarrHostConfigCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets or sets a value that indicates whether the settings show the API key and the passwords.
	/// </summary>
	/// <value>The switch, which is present to show the API key, the password, and the proxy's password.</value>
	[Parameter]
	public SwitchParameter IncludeSensitiveInfo { get; set; }

	/// <summary>
	/// Gets the host settings and writes them to the pipeline.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		MetadataTag tag = provider.GetMetadataTag(Meta.HostConfig);
		this.WriteHostObject(tag, this.IncludeSensitiveInfo.ToBool());
	}

	/// <summary>
	/// Gets the host settings from the endpoint of the specified tag and writes them to the pipeline, with or without
	/// their sensitive values.
	/// </summary>
	/// <remarks>
	/// When the request fails, the method writes its error, unless the error is ignorable.
	/// </remarks>
	/// <param name="tag">The metadata tag of the host config endpoint.</param>
	/// <param name="includeApiKey">
	/// <see langword="true"/> to write a <see cref="HostObject"/> that shows the API key and the passwords; otherwise,
	/// <see langword="false"/> to write a <see cref="NoKeyHostObject"/> that hides them.
	/// </param>
	private void WriteHostObject(MetadataTag tag, bool includeApiKey)
	{
		if (includeApiKey)
		{
			var response = this.GetHostResponse<HostObject>(tag);
			_ = this.TryWriteObject(response);
		}
		else
		{
			var response = this.GetHostResponse<NoKeyHostObject>(tag);
			_ = this.TryWriteObject(response);
		}
	}

	/// <summary>
	/// Gets the host settings from the endpoint of the specified tag as the specified type.
	/// </summary>
	/// <typeparam name="THost">The model that the settings are read as.</typeparam>
	/// <param name="tag">The metadata tag of the host config endpoint.</param>
	/// <returns>The response, which holds the settings or the request's error.</returns>
	private SonarrClientResult<THost> GetHostResponse<THost>(MetadataTag tag) where THost : HostObject
	{
		return this.SendGetRequest<THost>(tag.UrlBase);
	}
}
