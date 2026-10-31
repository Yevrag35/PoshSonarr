using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.System;

/// <summary>
/// Represents a drive on the Sonarr server and its free and total space, from <c>/diskspace</c>.
/// </summary>
/// <remarks>
/// <para>
/// A drive that Sonarr sends without a label gets an empty <c>Label</c> property, so every drive shows one.
/// </para>
/// <para>
/// Drives sort by path, ignoring case.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class DiskspaceObject : SonarrObject,
	IComparable<DiskspaceObject>,
	ISerializableNames<DiskspaceObject>
{
	/// <summary>
	/// The number of properties that a new drive reserves room for.
	/// </summary>
	const int CAPACITY = 6;
	/// <summary>
	/// The name of the property that holds the drive's label.
	/// </summary>
	const string LABEL = "Label";
	/// <summary>
	/// The full name of the type, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(DiskspaceObject).GetName();

	/// <summary>
	/// Gets the drive's free space.
	/// </summary>
	/// <remarks>
	/// It's a copy of the <c>FreeSpace</c> property, taken while the drive is deserialized.
	/// </remarks>
	/// <value>The free space, or 0 bytes when Sonarr sends none.</value>
	public Size FreeSpace { get; private set; }
	/// <summary>
	/// Gets the drive's path.
	/// </summary>
	/// <remarks>
	/// It's a copy of the <c>Path</c> property, taken while the drive is deserialized.
	/// </remarks>
	/// <value>The path of the drive's mount point, or an empty string when Sonarr sends none.</value>
	public string Path { get; private set; } = string.Empty;
	/// <summary>
	/// Gets the drive's total space.
	/// </summary>
	/// <remarks>
	/// It's a copy of the <c>TotalSpace</c> property, taken while the drive is deserialized.
	/// </remarks>
	/// <value>The total space, or 0 bytes when Sonarr sends none.</value>
	public Size TotalSpace { get; private set; }

	/// <summary>
	/// Initializes a new <see cref="DiskspaceObject"/> instance with no properties.
	/// </summary>
	public DiskspaceObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Compares this drive with another by path.
	/// </summary>
	/// <param name="other">The drive to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this drive sorts before <paramref name="other"/>, 0 if they sort together, or a positive
	/// number if it sorts after. Paths compare by the invariant culture, ignoring case.
	/// </returns>
	public int CompareTo(DiskspaceObject? other)
	{
		return StringComparer.InvariantCultureIgnoreCase.Compare(this.Path, other?.Path);
	}
	/// <summary>
	/// Gets the metadata tag for drives.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's tags.</param>
	/// <param name="existing">The tag that the object has now, which a drive doesn't use.</param>
	/// <returns>The resolver's tag for the disk space endpoint.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.DISK];
	}

	/// <summary>
	/// Finishes deserializing the drive.
	/// </summary>
	/// <remarks>
	/// It runs the base's deserialization, adds an empty <c>Label</c> property when the drive has none, and copies
	/// the drive's path and sizes into <see cref="Path"/>, <see cref="FreeSpace"/>, and <see cref="TotalSpace"/>.
	/// </remarks>
	public override void OnDeserialized()
	{
		base.OnDeserialized();
		PSPropertyInfo? label = this.Properties[LABEL];
		if (label is null)
		{
			this.Properties.Add(new StringNoteProperty(LABEL, string.Empty));
		}

		if (this.TryGetNonNullProperty(nameof(this.Path), out string? path))
		{
			this.Path = path;
		}

		if (this.TryGetProperty(nameof(this.FreeSpace), out long fs))
		{
			this.FreeSpace = new Size(in fs);
		}

		if (this.TryGetProperty(nameof(this.TotalSpace), out long ts))
		{
			this.TotalSpace = new Size(in ts);
		}
	}
	/// <summary>
	/// Inserts the drive's own type name ahead of the base type names.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}
}

