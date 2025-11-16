using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Fields;

[SonarrObject]
public sealed class SelectOptionObject : SonarrObject,
	IComparable<SelectOptionObject>,
	ISerializableNames<SelectOptionObject>
{
	static readonly string _typeName = typeof(SelectOptionObject).GetName();

	const int CAPACITY = 3;
	protected override bool DisregardMetadataTag => true;

	public string Name => this.GetStringOrEmpty();
	public int Order => this.GetValue<int>();

	public SelectOptionObject()
		: base(CAPACITY)
	{
	}

	public int CompareTo(SelectOptionObject? other)
	{
		return Comparer<int?>.Default.Compare(this.Order, other?.Order);
	}
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return existing;
	}

	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
	protected override void OnDeserialized(bool alreadyCalled)
	{
		this.ReplaceWithReadOnlyStringProperty(nameof(Name));
		this.ReplaceWithReadOnlyNumberProperty<int>(nameof(Order));
	}

	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}
}

