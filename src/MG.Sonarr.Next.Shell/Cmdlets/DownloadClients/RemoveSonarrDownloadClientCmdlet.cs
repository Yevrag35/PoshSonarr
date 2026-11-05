using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.DownloadClients;

/// <summary>
/// Represents the <c>Remove-SonarrDownloadClient</c> cmdlet, which deletes download clients from Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes the clients' IDs from <c>-Id</c>, or <see cref="DownloadClientObject"/> instances from the pipeline,
/// as <c>Get-SonarrDownloadClient</c> writes them. It collects the IDs, and once the input ends, it sends a request that
/// deletes each client, once for each ID, in ascending order of ID. It writes nothing to the pipeline, and writes a
/// verbose message for each client that it deletes.
/// </para>
/// <para>
/// Its confirm impact is High, so it confirms each client before it deletes it. <c>-Force</c> skips that prompt, but
/// not <c>-WhatIf</c> or an explicit <c>-Confirm</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Remove, "SonarrDownloadClient", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true,
	DefaultParameterSetName = PSConstants.PSetExplicitId)]
public sealed class RemoveSonarrDownloadClientCmdlet : SonarrMetadataCmdlet
{
	SortedSet<int> _ids = null!;

	/// <summary>
	/// Gets or sets the IDs of the download clients to delete.
	/// </summary>
	/// <remarks>
	/// Each ID has to be greater than zero. The parameter can't be combined with <c>-InputObject</c>.
	/// </remarks>
	/// <value>The IDs of the clients. The default is an empty array.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the download clients to delete.
	/// </summary>
	/// <remarks>
	/// The parameter takes the clients from the pipeline, and each one's ID has to be greater than zero. It can't be
	/// combined with <c>-Id</c>.
	/// </remarks>
	/// <value>The clients, as <c>Get-SonarrDownloadClient</c> writes them. The default is an empty array.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public DownloadClientObject[] InputObject { get; set; } = [];

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet deletes the download clients without confirming each one.
	/// </summary>
	/// <remarks>
	/// The switch skips the confirmation prompt only: in What if mode the cmdlet still deletes nothing, and an explicit
	/// <c>-Confirm</c> still prompts.
	/// </remarks>
	/// <value>The switch, which is present to skip the confirmation prompt.</value>
	[Parameter]
	public SwitchParameter Force { get; set; }

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
	/// Runs the base class's setup for the scope, then creates the set of IDs that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
	}

	/// <summary>
	/// Adds the values of <c>-Id</c> to the IDs of the download clients to delete.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}
	/// <summary>
	/// Adds the IDs of the download clients from the pipeline to the IDs of the clients to delete.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Process(IServiceProvider provider)
	{
		if (this.HasParameter(InputObject) && this.InputObject.Length > 0)
		{
			foreach (DownloadClientObject obj in this.InputObject)
			{
				_ = _ids.Add(obj.Id);
			}
		}
	}
	/// <summary>
	/// Deletes the download client with each of the collected IDs, in ascending order of ID.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		if (_ids.Count <= 0)
		{
			return;
		}

		bool force = this.Force.ToBool();
		foreach (int id in _ids)
		{
			this.DeleteDownloadClient(id, this.Tag, force);
		}
	}

	/// <summary>
	/// Deletes the download client with the specified ID once the action is confirmed, and writes a verbose message when
	/// it's deleted.
	/// </summary>
	/// <remarks>
	/// When the request fails, the method writes its error, whatever the error is.
	/// </remarks>
	/// <param name="id">The ID of the client to delete.</param>
	/// <param name="tag">The metadata tag of the download client endpoint, which builds the client's URL.</param>
	/// <param name="force"><see langword="true"/> to skip the confirmation prompt; otherwise, <see langword="false"/>.</param>
	private void DeleteDownloadClient(int id, MetadataTag tag, bool force)
	{
		string url = tag.GetUrlForId(id);
		if (!this.ShouldProcessWithForce(url, "Delete Download Client", force))
		{
			return;
		}

		var response = this.SendDeleteRequest(url);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			return;
		}

		this.WriteVerbose($"Deleted Download Client -> {id}");
	}
}

