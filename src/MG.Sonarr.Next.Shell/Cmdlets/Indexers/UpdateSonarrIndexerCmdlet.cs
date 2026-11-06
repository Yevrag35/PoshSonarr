using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Models.Indexers;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Indexers;

/// <summary>
/// Represents the <c>Update-SonarrIndexer</c> cmdlet, which saves changes made to indexers back to Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes an <see cref="IndexerObject"/>, as <c>Get-SonarrIndexer</c> writes it, from the pipeline or by
/// name, and sends it to Sonarr, replacing the indexer that has its ID. It writes nothing to the pipeline.
/// </para>
/// <para>
/// The cmdlet confirms each indexer before it sends it, and its confirm impact is Low, so it prompts only under
/// <c>-Confirm</c> or a <c>$ConfirmPreference</c> of Low.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Set-SonarrIndexer</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsData.Update, "SonarrIndexer", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
[Alias("Set-SonarrIndexer")]
[MetadataCanPipe(Tag = Meta.Indexer)]
public sealed class UpdateSonarrIndexerCmdlet : SonarrApiCmdletBase
{
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
	/// Gets or sets the indexer to save.
	/// </summary>
	/// <remarks>
	/// The parameter takes the indexer from the pipeline, and its ID has to be greater than zero.
	/// </remarks>
	/// <value>The indexer, with the changes to send.</value>
	[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
	[ValidateId(ValidateRangeKind.Positive)]
	[ValidateNotNull]
	public IndexerObject InputObject { get; set; } = null!;

	/// <summary>
	/// Sends the indexer to Sonarr once the action is confirmed, and commits or resets its properties as the request
	/// succeeds or fails.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Serializing the indexer runs its <see cref="IndexerObject.OnSerializing"/>, which only the
	/// <see cref="MG.Sonarr.Next.Models.SonarrObject.Commit"/> or <see cref="MG.Sonarr.Next.Models.SonarrObject.Reset"/>
	/// after the request undoes, so the cmdlet writes the body to the debug stream only once
	/// <see cref="Cmdlet.ShouldProcess(string, string)"/> has agreed to send it, where <c>-WhatIf</c> doesn't reach.
	/// </para>
	/// <para>
	/// When the request fails, the cmdlet resets the indexer and then writes the request's error, unless the error is
	/// ignorable. It resets first because under <c>-ErrorAction Stop</c>, writing the error ends the statement, and the
	/// indexer would keep the tags that the request failed to save.
	/// </para>
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		string path = this.InputObject.MetadataTag.GetUrlForId(this.InputObject.Id);
		if (this.ShouldProcess(path, "Update Indexer"))
		{
			this.SerializeIfDebug(this.InputObject);

			var response = this.SendPutRequest(path, this.InputObject);
			if (response.IsError)
			{
				this.InputObject.Reset();
				this.WriteConditionalError(response.Error);
			}
			else
			{
				this.InputObject.Commit();
			}
		}
	}
}
