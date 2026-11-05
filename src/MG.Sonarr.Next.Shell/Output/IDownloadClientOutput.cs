using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Fields;
using System.Collections.Immutable;

namespace MG.Sonarr.Next.Shell.Output;

/// <summary>
/// Represents the properties of the download clients that <c>Get-SonarrDownloadClient</c> and
/// <c>New-SonarrDownloadClient</c> write, for the cmdlets' <see cref="OutputTypeAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// No model implements the interface. The cmdlets write each download client as a
/// <see cref="MG.Sonarr.Next.Models.DownloadClients.DownloadClientObject"/>, whose PowerShell properties come from the
/// fields of Sonarr's JSON for a download client, and the interface names the ones that a client has.
/// </para>
/// <para>
/// The <c>Id</c> property is the client's ID, and <c>Tags</c> holds the IDs of its tags.
/// </para>
/// </remarks>
public interface IDownloadClientOutput : IHasId, ITagOutput
{
	/// <summary>
	/// Gets the name of the contract that the client's settings follow.
	/// </summary>
	/// <value>The name of the implementation's settings contract.</value>
	string ConfigContract { get; }
	/// <summary>
	/// Gets a value that indicates whether the client is enabled.
	/// </summary>
	/// <value><see langword="true"/> if Sonarr uses the client; otherwise, <see langword="false"/>.</value>
	bool Enable { get; }
	/// <summary>
	/// Gets the client's settings.
	/// </summary>
	/// <value>The fields that hold the client's settings, in the order that Sonarr sent them.</value>
	ImmutableArray<FieldObject> Fields { get; }
	/// <summary>
	/// Gets the name of the client's implementation.
	/// </summary>
	/// <value>The name of the kind of download client that Sonarr talks to.</value>
	string Implementation { get; }
	/// <summary>
	/// Gets the name that Sonarr shows for the client's implementation.
	/// </summary>
	/// <value>The implementation's display name.</value>
	string ImplementationName { get; }
	/// <summary>
	/// Gets the link to the documentation of the client's implementation.
	/// </summary>
	/// <value>The URL of the implementation's documentation.</value>
	string InfoLink { get; }
	/// <summary>
	/// Gets the client's name.
	/// </summary>
	/// <value>The name of the download client.</value>
	string Name { get; }
	/// <summary>
	/// Gets the client's priority among the clients of its protocol.
	/// </summary>
	/// <value>The client's priority, where a lower number comes first.</value>
	int Priority { get; }
	/// <summary>
	/// Gets the protocol of the releases that the client downloads.
	/// </summary>
	/// <value>The client's protocol, such as <c>usenet</c> or <c>torrent</c>.</value>
	string Protocol { get; }
	/// <summary>
	/// Gets a value that indicates whether Sonarr removes downloads from the client once they're imported.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if Sonarr removes completed downloads; otherwise, <see langword="false"/>.
	/// </value>
	bool RemoveCompletedDownloads { get; }
	/// <summary>
	/// Gets a value that indicates whether Sonarr removes failed downloads from the client.
	/// </summary>
	/// <value><see langword="true"/> if Sonarr removes failed downloads; otherwise, <see langword="false"/>.</value>
	bool RemoveFailedDownloads { get; }
}
