using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes;

[Cmdlet(VerbsCommon.Remove, "SonarrEpisodeFile", SupportsShouldProcess = true)]
[Alias("Delete-SonarrEpisodeFile")]
[MetadataCanPipe(Tag = Meta.Calendar)]
[MetadataCanPipe(Tag = Meta.Episode)]
[MetadataCanPipe(Tag = Meta.EpisodeFile)]
public sealed class RemoveSonarrEpisodeFileCmdlet : SonarrMetadataCmdlet
{
	SortedSet<int> _ids = null!;

	[Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByEpisodeId")]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipelineInput")]
	[ValidateIds(ValidateRangeKind.NonNegative, typeof(IEpisodeFilePipeable))]
	public IEpisodeFilePipeable[] InputObject { get; set; } = [];

	[Parameter]
	public SwitchParameter Force { get; set; }

	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.EpisodeFile];
	}
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
	}

	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Process(IServiceProvider provider)
	{
		if (this.HasParameter(InputObject) && this.InputObject.Length > 0)
		{
			EpisodeFileInput.AddFileIds(this, this.InputObject, _ids);
		}
	}
	protected override void End(IServiceProvider provider)
	{
		bool yesToAll = false;
		bool noToAll = false;

		bool force = this.Force.ToBool();

		foreach (int id in _ids)
		{
			string url = this.Tag.GetUrlForId(id);
			if (this.ShouldProcess(url, "Deleting Episode File")
				&&
				(force
				||
				this.ShouldContinue(
					query: "Are you sure you want to delete the episode file?",
					caption: $"Delete Episode file: {id}",
					yesToAll: ref yesToAll,
					noToAll: ref noToAll)))
			{
				this.DeleteEpisodeFile(url);
			}
		}
	}

	private void DeleteEpisodeFile(string url)
	{
		var response = this.SendDeleteRequest(url);
		if (response.IsError)
		{
			if (response.Error.IsIgnorable)
			{
				this.WriteWarning(response.Error.Message);
			}
			else
			{
				this.WriteError(response.Error);
			}
		}
	}
}
