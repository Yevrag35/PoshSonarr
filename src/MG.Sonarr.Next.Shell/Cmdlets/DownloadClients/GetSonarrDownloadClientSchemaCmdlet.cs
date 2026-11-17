using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.DownloadClients;

/// <summary>
/// Represents the <c>Get-SonarrDownloadClientSchema</c> cmdlet, which gets the templates that new download clients are
/// created from.
/// </summary>
/// <remarks>
/// <para>
/// Sonarr returns a template for each kind of download client that it supports, and the cmdlet writes each one to the
/// pipeline as a <see cref="DownloadClientSchemaObject"/>, which <c>New-SonarrDownloadClient</c> takes from the
/// pipeline.
/// </para>
/// <para>
/// <c>-Name</c> limits the templates to those whose implementation name matches one of the names.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrDownloadClientSchema")]
public sealed class GetSonarrDownloadClientSchemaCmdlet : SonarrMetadataCmdlet
{
	private WildcardSet _wcNames = null!;

	/// <summary>
	/// Gets or sets the implementation names of the templates to get.
	/// </summary>
	/// <remarks>
	/// A name can hold wildcards and matches a template's implementation name, such as <c>qBittorrent</c>, ignoring case.
	/// </remarks>
	/// <value>The names of the templates. The default is an empty array, for every template.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = false, Position = 0)]
	[SupportsWildcards]
	public string[] Name { get; set; } = [];

	/// <summary>
	/// Gets the metadata tag of the download client schema endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The download client schema's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.DownloadClientSchema];
	}

	/// <summary>
	/// Runs the base class's setup for the scope, then creates the set of names that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_wcNames = new();
	}

	/// <summary>
	/// Gets every template and writes to the pipeline those whose implementation name matches one of the names in
	/// <c>-Name</c>, or every template when there are none.
	/// </summary>
	/// <remarks>
	/// When the request fails, the cmdlet writes its error, unless the error is ignorable, and writes no templates.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		base.Process(provider);
		_wcNames.UnionWith(this.Name);
		var all = this.SendGetRequest<MetadataList<DownloadClientSchemaObject>>(this.Tag.UrlBase);

		if (all.IsError)
		{
			this.WriteConditionalError(all.Error);
			return;
		}

		IEnumerable<DownloadClientSchemaObject> results = _wcNames.Count > 0
			? all.Value.Where(x => _wcNames.IsAnyMatch(x.ImplementationName))
			: all.Value;

		this.WriteCollection(results);
	}
}