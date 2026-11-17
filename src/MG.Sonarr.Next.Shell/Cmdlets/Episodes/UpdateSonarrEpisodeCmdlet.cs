using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes;

/// <summary>
/// Represents the <c>Update-SonarrEpisode</c> cmdlet, which saves the changes made to episode objects back to Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes episodes that <c>Get-SonarrEpisode</c> wrote, after their properties were changed in
/// PowerShell, from the pipeline or through <c>-InputObject</c>, and sends each confirmed episode to Sonarr with a
/// PUT request as it arrives. It writes nothing to the pipeline. After a request that succeeds, the episode is
/// committed, and after one that fails, the cmdlet writes the request's error, resets the episode, and goes on to the
/// next.
/// </para>
/// <para>
/// The cmdlet confirms each episode before it sends it. Its confirm impact is low, so it prompts only when
/// <c>-Confirm</c> or <c>$ConfirmPreference</c> asks for it, and <c>-WhatIf</c> shows what it would update and sends
/// nothing.
/// </para>
/// </remarks>
[Cmdlet(VerbsData.Update, "SonarrEpisode", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
[MetadataCanPipe(Tag = Meta.Episode)]
public sealed class UpdateSonarrEpisodeCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets or sets the episodes whose changes the cmdlet saves.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, and each piped episode arrives in an array of its own. Each episode's ID
	/// has to be greater than zero.
	/// </remarks>
	/// <value>The changed episodes. The default is an empty array.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public EpisodeObject[] InputObject { get; set; } = [];

	/// <summary>
	/// Sends each episode from <c>-InputObject</c> to Sonarr, and writes a warning for one whose ID isn't greater than
	/// zero, which it leaves out.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		if (this.InputObject.Length <= 0)
		{
			return;
		}

		foreach (var ep in this.InputObject)
		{
			if (this.IsValid(ep))
			{
				this.SendUpdate(ep);
			}
		}
	}

	/// <summary>
	/// Determines whether the specified episode has an ID that the cmdlet can send, and writes a warning when it
	/// doesn't.
	/// </summary>
	/// <param name="value">The episode, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="value"/> isn't <see langword="null"/> and its ID is greater than zero;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private bool IsValid([NotNullWhen(true)] IHasId? value)
	{
		if (value is null or { Id: <= 0 })
		{
			this.WriteWarning("An episode with an invalid ID was passed. It will be ignored.");
			return false;
		}

		return true;
	}
	/// <summary>
	/// Sends the specified episode to its endpoint with a PUT request, once it's confirmed, and commits or resets it
	/// as the response says.
	/// </summary>
	/// <remarks>
	/// The endpoint's path comes from the episode's own metadata tag and ID. After a request that fails, the method
	/// writes its error.
	/// </remarks>
	/// <param name="episode">The episode to send.</param>
	private void SendUpdate(EpisodeObject episode)
	{
		Debug.Assert(episode is not null, "Episode should not be null here.");
		string url = episode.MetadataTag.GetUrlForId(episode.Id);
		if (!this.ShouldProcess(url, "Update Episode"))
		{
			return;
		}

		var response = this.SendPutRequest(url, episode);
		this.TryCommitFromResponse(episode, response);
	}
}
