using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Delay;

/// <summary>
/// Represents the <c>Update-SonarrDelayProfile</c> cmdlet, which saves changes to delay profiles in Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes <see cref="DelayProfileObject"/> instances, such as those that <c>Get-SonarrDelayProfile</c> writes
/// with their properties changed, from the pipeline or from <c>-InputObject</c>, and sends Sonarr a request for each
/// that replaces the stored profile with it. It writes nothing to the pipeline.
/// </para>
/// <para>
/// Serializing a profile for its request can change the profile, so after each request the cmdlet brings the profile
/// back in step: it commits the profile when the update succeeds, and resets it and writes the error when it fails.
/// </para>
/// <para>
/// The cmdlet confirms each update, and its confirm impact is low, so it prompts only when <c>-Confirm</c> or
/// <c>$ConfirmPreference</c> asks it to.
/// </para>
/// </remarks>
[Cmdlet(VerbsData.Update, "SonarrDelayProfile", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
[MetadataCanPipe(Tag = Meta.DelayProfile)]
public sealed class UpdateSonarrDelayProfileCmdlet : SonarrMetadataCmdlet
{
	/// <summary>
	/// Gets or sets the delay profiles to update.
	/// </summary>
	/// <remarks>
	/// Each profile's ID has to be greater than zero.
	/// </remarks>
	/// <value>The changed profiles to send to Sonarr.</value>
	[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public DelayProfileObject[] InputObject { get; set; } = Array.Empty<DelayProfileObject>();

	/// <summary>
	/// Gets the metadata tag of the delay profile endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The delay profile's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.DelayProfile];
	}

	/// <summary>
	/// Updates each profile in <c>-InputObject</c>.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		foreach (DelayProfileObject profile in this.InputObject)
		{
			this.UpdateProfile(profile, this.Tag);
		}
	}

	/// <summary>
	/// Sends the specified profile to Sonarr once the action is confirmed, then commits its properties when the request
	/// succeeds, or resets them and writes the request's error when it fails.
	/// </summary>
	/// <param name="profile">The profile to send.</param>
	/// <param name="tag">The metadata tag of the delay profile endpoint.</param>
	private void UpdateProfile(DelayProfileObject profile, MetadataTag tag)
	{
		string url = tag.GetUrlForId(profile.Id);
		if (!this.ShouldProcess(url, "Update Delay Profile"))
		{
			return;
		}

		var response = this.SendPutRequest(url, profile);
		_ = this.TryCommitFromResponse(profile, response);
	}
}

