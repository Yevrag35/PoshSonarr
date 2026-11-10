using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Output;

namespace MG.Sonarr.Next.Shell.Cmdlets.DownloadClients;

/// <summary>
/// Represents the <c>New-SonarrDownloadClient</c> cmdlet, which creates a download client from a template.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes a template from <c>Get-SonarrDownloadClientSchema</c>, from the pipeline or by name, and sends a
/// copy of it, with the name, priority, and enabled state that <c>-Name</c>, <c>-Priority</c>, and <c>-Enabled</c>
/// give, so the template itself is left unchanged. Sonarr returns the new client, and the cmdlet writes it to the
/// pipeline as a <see cref="DownloadClientObject"/>.
/// </para>
/// <para>
/// A client needs a name, so when neither <c>-Name</c> nor the template gives one, the cmdlet writes an error and
/// sends nothing. <c>-ForceSave</c> asks Sonarr to save the client even when its test of the client's settings fails.
/// </para>
/// <para>
/// The cmdlet confirms each client before it creates it, and its confirm impact is Low, so it prompts only under
/// <c>-Confirm</c> or a <c>$ConfirmPreference</c> of Low.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.New, "SonarrDownloadClient", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
[MetadataCanPipe(Tag = Meta.DownloadClientSchema), OutputType(typeof(IDownloadClientOutput))]
public sealed class NewSonarrDownloadClientCmdlet : SonarrMetadataCmdlet
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
	/// Gets or sets the template that the new download client is created from.
	/// </summary>
	/// <remarks>
	/// The parameter takes a template from the pipeline. The cmdlet sends a copy of it, so the template keeps its own
	/// values.
	/// </remarks>
	/// <value>The template, from <c>Get-SonarrDownloadClientSchema</c>.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true)]
	public DownloadClientSchemaObject Schema { get; set; } = null!;

	/// <summary>
	/// Gets or sets a value that indicates whether the new download client is enabled.
	/// </summary>
	/// <remarks>
	/// When the switch isn't present, the client takes the template's enabled state.
	/// </remarks>
	/// <value>The switch, which is present to enable the client.</value>
	[Parameter]
	public SwitchParameter Enabled { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether Sonarr saves the new download client even when its test of the
	/// client's settings fails.
	/// </summary>
	/// <value>The switch, which is present to save the client whatever its test finds.</value>
	[Parameter]
	public SwitchParameter ForceSave { get; set; }

	/// <summary>
	/// Gets or sets the new download client's name.
	/// </summary>
	/// <value>
	/// The client's name, or <see langword="null"/> to keep the template's. The default is <see langword="null"/>.
	/// </value>
	[Parameter]
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets the new download client's priority.
	/// </summary>
	/// <value>
	/// The client's priority, or <see langword="null"/> to keep the template's. The default is <see langword="null"/>.
	/// </value>
	[Parameter]
	public int? Priority { get; set; }

	/// <summary>
	/// Gets the metadata tag of the download client endpoint, which the new client is posted to.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The download client's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.DownloadClient];
	}

	/// <summary>
	/// Copies the template, sets the copy's name, priority, and enabled state from the parameters, and posts it to
	/// Sonarr, writing the new download client to the pipeline.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When the copy has no name, or one of only white space, the cmdlet writes an error with the category
	/// <see cref="ErrorCategory.InvalidData"/> and sends nothing for this template.
	/// </para>
	/// <para>
	/// The copy's JSON goes to the debug stream before the cmdlet confirms the request. A template has nothing to undo
	/// after it's serialized, and the copy goes no further than this call, so <c>-WhatIf</c> still leaves nothing
	/// changed. When the request fails, the cmdlet writes its error, unless the error is ignorable.
	/// </para>
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
	protected override void Process(IServiceProvider provider)
	{
		DownloadClientSchemaObject schema = (DownloadClientSchemaObject)this.Schema.Copy();
		if (this.HasNotNullParameter(Name))
		{
			schema.Name = this.Name;
		}

		if (this.HasParameter(Priority) && this.Priority.HasValue)
		{
			schema.Priority = this.Priority.Value;
		}

		if (this.Enabled.IsPresent && this.Enabled != schema.Enable)
		{
			schema.Enable = this.Enabled;
		}

		if (string.IsNullOrWhiteSpace(schema.Name))
		{
			var ex = new MalformedSonarrObjectException("The download client schema must have a valid Name.", schema);
			this.WriteError(new ErrorRecord(ex, "New-SonarrDownloadClient.InvalidDownloadClientName", ErrorCategory.InvalidData, schema));
			return;
		}

		this.SerializeIfDebug(schema, includeType: true);

		if (this.ShouldProcess(this.Tag.UrlBase, $"Create new download client of type '{schema.ImplementationName}' -> '{schema.Name}'"))
		{
			string url = this.ForceSave.ToBool()
				? this.Tag.GetUrl(BooleanQueryField.CreateTrue("forceSave"))
				: this.Tag.UrlBase;

			var response = this.SendPostRequest<DownloadClientObject>(url, schema);

			this.WriteOutcome(response);
		}
	}
}
