using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RemotePaths;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.RemotePath;

/// <summary>
/// Represents the <c>Update-SonarrRemotePathMapping</c> cmdlet, which changes the host, remote path, or local path of
/// a remote path mapping in Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// <c>-Id</c> names the mapping, and <c>-HostName</c>, <c>-RemotePath</c>, and <c>-LocalPath</c> give its new values.
/// Sonarr replaces the whole mapping, so when one of the three isn't given, the cmdlet first gets the mapping and keeps
/// the value that it has. It writes nothing to the pipeline, and writes a verbose message for each mapping that it
/// updates.
/// </para>
/// <para>
/// Each parameter also binds by property name, so a <see cref="RemotePathObject"/> from
/// <c>Get-SonarrRemotePathMapping</c>, or any object with the same properties, can be piped to the cmdlet.
/// </para>
/// <para>
/// The cmdlet confirms each mapping before it sends it, and its confirm impact is Low, so it prompts only under
/// <c>-Confirm</c> or a <c>$ConfirmPreference</c> of Low.
/// </para>
/// <para>
/// The cmdlet's aliases are <c>Update-SonarrRemoteMapping</c>, <c>Set-SonarrRemotePathMapping</c>, and
/// <c>Set-SonarrRemoteMapping</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsData.Update, "SonarrRemotePathMapping", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
[Alias("Update-SonarrRemoteMapping", "Set-SonarrRemotePathMapping", "Set-SonarrRemoteMapping")]
[MetadataCanPipe(Tag = Meta.RemotePathMapping)]
public sealed class UpdateSonarrRemotePathMappingCmdlet : SonarrMetadataCmdlet
{
	RemotePathBody? _body;

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
	/// Gets or sets the new host of the download client that the mapping applies to.
	/// </summary>
	/// <remarks>
	/// The parameter binds by property name from the pipeline. A value of only white space is ignored, as though it
	/// wasn't given.
	/// </remarks>
	/// <value>The host, or an empty string to keep the mapping's. The default is an empty string.</value>
	[Parameter(ValueFromPipelineByPropertyName = true)]
	[ValidateNotNullOrEmpty]
	public string HostName
	{
		get => _body?.Host ?? string.Empty;
		set => this.SetBodyParameter(value, (x, s) => x.Host = s);
	}

	/// <summary>
	/// Gets or sets the ID of the remote path mapping to update.
	/// </summary>
	/// <remarks>
	/// The ID has to be greater than zero. The parameter binds by property name from the pipeline.
	/// </remarks>
	/// <value>The mapping's ID. The default is 0.</value>
	[Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int Id
	{
		get => _body?.Id ?? 0;
		set => this.SetBodyParameter(value);
	}

	/// <summary>
	/// Gets or sets the new path that Sonarr uses in place of the remote path.
	/// </summary>
	/// <remarks>
	/// The parameter binds by property name from the pipeline. A value of only white space is ignored, as though it
	/// wasn't given.
	/// </remarks>
	/// <value>The local path, or an empty string to keep the mapping's. The default is an empty string.</value>
	[Parameter(ValueFromPipelineByPropertyName = true)]
	[ValidateNotNullOrEmpty]
	public string LocalPath
	{
		get => _body?.LocalPath ?? string.Empty;
		set => this.SetBodyParameter(value, (x, s) => x.LocalPath = s);
	}

	/// <summary>
	/// Gets or sets the new path that the download client reports.
	/// </summary>
	/// <remarks>
	/// The parameter binds by property name from the pipeline. A value of only white space is ignored, as though it
	/// wasn't given.
	/// </remarks>
	/// <value>The remote path, or an empty string to keep the mapping's. The default is an empty string.</value>
	[Parameter(ValueFromPipelineByPropertyName = true)]
	[ValidateNotNullOrEmpty]
	public string RemotePath
	{
		get => _body?.RemotePath ?? string.Empty;
		set => this.SetBodyParameter(value, (x, s) => x.RemotePath = s);
	}

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
	/// Fills in the values that weren't given from the mapping as Sonarr has it, and sends the mapping to Sonarr once
	/// the action is confirmed.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The body's JSON goes to the debug stream before the cmdlet confirms the request. The body is a
	/// <see cref="RemotePathBody"/>, which serializing leaves unchanged, so writing it first changes nothing, under
	/// <c>-WhatIf</c> either.
	/// </para>
	/// <para>
	/// When getting the mapping or sending it fails, the cmdlet writes the request's error, whatever the error is, and
	/// sends nothing more for this input.
	/// </para>
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		if (_body is null)
		{
			return;
		}

		string url = this.Tag.GetUrlForId(this.Id);

		// Sonarr replaces the whole mapping, so a value that wasn't passed keeps the one that the mapping has.
		if (!this.TryFillMissingValues(url, _body))
		{
			return;
		}

		this.SerializeIfDebug(_body);

		if (!this.ShouldProcess(url, "Updating Remote Path Mapping"))
		{
			return;
		}

		var response = this.SendPutRequest(url, _body);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			return;
		}

		this.WriteVerbose($"Updated Remote Path Mapping -> {this.Id}");
	}

	/// <summary>
	/// Fills in the host, local path, and remote path that the specified body lacks from the mapping at the specified
	/// URL.
	/// </summary>
	/// <remarks>
	/// The method sends a request only when the body lacks one of the three. When that request fails, it writes the
	/// request's error, whatever the error is.
	/// </remarks>
	/// <param name="url">The mapping's URL, relative to the API.</param>
	/// <param name="body">The body to send, which the method fills in.</param>
	/// <returns>
	/// <see langword="true"/> if the body has every value; otherwise, <see langword="false"/>, when the request for the
	/// mapping failed.
	/// </returns>
	private bool TryFillMissingValues(string url, RemotePathBody body)
	{
		bool noHost = string.IsNullOrEmpty(body.Host);
		bool noLocal = string.IsNullOrEmpty(body.LocalPath);
		bool noRemote = string.IsNullOrEmpty(body.RemotePath);
		if (!noHost && !noLocal && !noRemote)
		{
			return true;
		}

		var existing = this.SendGetRequest<RemotePathObject>(url);
		if (existing.IsError)
		{
			this.WriteError(existing.Error);
			return false;
		}

		if (noHost)
		{
			body.Host = existing.Value.HostName;
		}

		if (noLocal)
		{
			body.LocalPath = existing.Value.LocalPath;
		}

		if (noRemote)
		{
			body.RemotePath = existing.Value.RemotePath;
		}

		return true;
	}
	/// <summary>
	/// Sets the ID in the body to send, creating the body when there isn't one yet.
	/// </summary>
	/// <remarks>
	/// An ID that isn't greater than zero is ignored.
	/// </remarks>
	/// <param name="id">The ID of the mapping to update.</param>
	private void SetBodyParameter(int id)
	{
		if (id <= 0)
		{
			return;
		}

		_body ??= new();
		_body.Id = id;
	}
	/// <summary>
	/// Sets a value in the body to send through the specified action, creating the body when there isn't one yet.
	/// </summary>
	/// <remarks>
	/// A value that's <see langword="null"/>, empty, or only white space is ignored.
	/// </remarks>
	/// <param name="value">The value to set.</param>
	/// <param name="action">The action that sets <paramref name="value"/> on the body.</param>
	private void SetBodyParameter(string value, Action<RemotePathBody, string> action)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return;
		}

		_body ??= new();
		action(_body, value);
	}
}

