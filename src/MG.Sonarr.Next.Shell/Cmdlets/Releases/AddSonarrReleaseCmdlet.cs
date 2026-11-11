using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Releases;

namespace MG.Sonarr.Next.Shell.Cmdlets.Releases;

/// <summary>
/// Represents the <c>Add-SonarrRelease</c> cmdlet, which has Sonarr grab releases that a release search found and
/// send them to a download client.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes the releases that <c>Search-SonarrRelease</c> writes, from the pipeline or through
/// <c>-InputObject</c>. For each one, it posts the release's GUID and indexer ID to Sonarr and writes what Sonarr
/// returns to the pipeline. When a post fails, the cmdlet writes its error and goes on to the next release.
/// </para>
/// <para>
/// The cmdlet confirms each release before it posts it. Its confirm impact is low, so it prompts only when
/// <c>-Confirm</c> or <c>$ConfirmPreference</c> asks for it, and <c>-WhatIf</c> shows what it would grab and grabs
/// nothing.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Add, "SonarrRelease", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
[MetadataCanPipe(Tag = Meta.Release)]
public sealed class AddSonarrReleaseCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets or sets the metadata tag of the release endpoint, which the cmdlet posts the releases to.
	/// </summary>
	/// <value>The <c>#release</c> tag, set when the cmdlet's scope is created.</value>
	private MetadataTag Tag { get; set; } = null!;

	/// <summary>
	/// Gets or sets the releases to grab.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, and each piped release arrives in an array of its own.
	/// </remarks>
	/// <value>The releases. The default is an empty array.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
	public ReleaseObject[] InputObject { get; set; } = Array.Empty<ReleaseObject>();

	/// <summary>
	/// Runs the base class's setup for the scope, then gets the metadata tag of the release endpoint.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		this.Tag = provider.GetRequiredService<IMetadataResolver>()[Meta.Release];
	}
	/// <summary>
	/// Posts each confirmed release from <c>-InputObject</c> to Sonarr, and writes Sonarr's response to the pipeline.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		foreach (ReleaseObject release in this.InputObject)
		{
			var post = ToPostRelease(release);

			if (this.ShouldProcess($"{post.Guid} from Indexer {post.IndexerId}", "Download Release"))
			{
				this.SendRelease(post);
			}
		}
	}

	/// <summary>
	/// Posts the specified release to the release endpoint, and writes the response's object to the pipeline, or the
	/// request's error when it fails.
	/// </summary>
	/// <param name="post">The body that names the release to grab.</param>
	private void SendRelease(PostRelease post)
	{
		var response = this.SendPostRequest<PSObject>(this.Tag.UrlBase, post);
		switch (response.Index)
		{
			case 1:
				this.WriteObject(response.AsT1);
				break;

			case 2:
				this.WriteError(response.AsT2);
				break;
		}
	}
	/// <summary>
	/// Creates the body that grabs the specified release, from its GUID and its indexer's ID.
	/// </summary>
	/// <param name="release">The release that a search found.</param>
	/// <returns>The body to post.</returns>
	private static PostRelease ToPostRelease(ReleaseObject release)
	{
		return new()
		{
			Guid = release.ReleaseUrl,
			IndexerId = release.IndexerId,
		};
	}
}
