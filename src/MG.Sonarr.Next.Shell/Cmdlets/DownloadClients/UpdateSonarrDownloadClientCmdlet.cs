using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.DownloadClients;

/// <summary>
/// Represents the <c>Update-SonarrDownloadClient</c> cmdlet, which saves changes made to download clients back to
/// Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes <see cref="DownloadClientObject"/> instances, as <c>Get-SonarrDownloadClient</c> writes them, from
/// the pipeline or by name, and sends each one to Sonarr as it is, replacing the client that has its ID. It writes
/// nothing to the pipeline.
/// </para>
/// <para>
/// The cmdlet confirms each client before it sends it, and its confirm impact is Low, so it prompts only under
/// <c>-Confirm</c> or a <c>$ConfirmPreference</c> of Low.
/// </para>
/// </remarks>
[Cmdlet(VerbsData.Update, "SonarrDownloadClient", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
[MetadataCanPipe(Tag = Meta.DownloadClient)]
public sealed class UpdateSonarrDownloadClientCmdlet : SonarrMetadataCmdlet
{
	/// <summary>
	/// Gets or sets the download clients to save.
	/// </summary>
	/// <remarks>
	/// The parameter takes the clients from the pipeline, and each one's ID has to be greater than zero.
	/// </remarks>
	/// <value>The clients, with the changes to send. The default is an empty array.</value>
	[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public DownloadClientObject[] InputObject { get; set; } = [];

	/// <summary>
	/// Gets the metadata tag of the download client endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The download client's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.DownloadClient];
	}

	/// <summary>
	/// Sends each of the download clients in <c>-InputObject</c> to Sonarr.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		foreach (DownloadClientObject dco in this.InputObject)
		{
			this.UpdateClient(dco, this.Tag);
		}
	}

	/// <summary>
	/// Sends the specified download client to Sonarr once the action is confirmed, and commits or resets its properties
	/// as the request succeeds or fails.
	/// </summary>
	/// <remarks>
	/// When the request fails, the method writes its error, whatever the error is.
	/// </remarks>
	/// <param name="client">The client to send.</param>
	/// <param name="tag">The metadata tag of the download client endpoint, which builds the client's URL.</param>
	private void UpdateClient(DownloadClientObject client, MetadataTag tag)
	{
		string url = tag.GetUrlForId(client.Id);
		if (!this.ShouldProcess(url, "Update Download Client"))
		{
			return;
		}

		var response = this.SendPutRequest(url, client);
		_ = this.TryCommitFromResponse(client, response);
	}
}

