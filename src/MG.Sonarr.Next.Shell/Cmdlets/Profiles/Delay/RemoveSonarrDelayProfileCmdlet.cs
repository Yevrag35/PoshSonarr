using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Exceptions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Delay;

[Cmdlet(VerbsCommon.Remove, "SonarrDelayProfile", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
[MetadataCanPipe(Tag = Meta.DELAY_PROFILE)]
public sealed class RemoveSonarrDelayProfileCmdlet : SonarrMetadataCmdlet
{
	SortedSet<int> _ids = null!;
	protected override int Capacity => 1;

	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
	[ValidateRange(2, int.MaxValue)]
	public int[] Id { get; set; } = [];

	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSET_PIPELINE)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public DelayProfileObject[] InputObject { get; set; } = [];

	[Parameter]
	public SwitchParameter Force { get; set; }

	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.DELAY_PROFILE];
	}
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = this.GetPooledObject<SortedSet<int>>();
		this.GetReturnables()[0] = _ids;
	}

	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Process(IServiceProvider provider)
	{
		if (this.InputObject.Length > 0)
		{
			_ids.UnionWith(this.InputObject.Select(x => x.Id));
		}
	}
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
	protected override void End(IServiceProvider provider)
	{
		if (_ids.Count == 0)
		{
			return;
		}
		else if (_ids.Contains(1))
		{
			this.WriteError(new ErrorRecord(new SonarrParameterException(nameof(Id), ParameterErrorType.Invalid, "Cannot delete a built-in Sonarr object."), "SonarrParameterException.DeletingRestrictedId", ErrorCategory.InvalidArgument, null));
			_ids.Remove(1);
		}

		bool force = this.Force.ToBool();
		foreach (int id in _ids)
		{
			this.DeleteProfile(in id, this.Tag, force);
		}
	}

	private void DeleteProfile(in int id, MetadataTag tag, bool force)
	{
		string url = tag.GetUrlForId(id);
		if (!force
			&&
			!this.ShouldProcess(url, "Delete Delay Profile"))
		{
			return;
		}

		var response = this.SendDeleteRequest(url);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			return;
		}

		this.WriteVerbose($"Deleted Delay Profile -> {id}");
	}
}

