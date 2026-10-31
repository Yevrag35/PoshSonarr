using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Qualities;

/// <summary>
/// Represents the revision of a release, which records whether it's a proper, a real, or a repack.
/// </summary>
/// <remarks>
/// Sonarr sends one as the <c>Revision</c> of a <see cref="QualityRevisionObject"/>, and C# builds one with
/// <see cref="Create(bool, bool)"/> or <see cref="Default"/>. Its <c>IsRepack</c>, <c>Real</c>, and
/// <c>Version</c> properties are read-only in PowerShell.
/// </remarks>
public sealed class RevisionObject : SonarrObject,
	IComparable<RevisionObject>,
	ISerializableNames<RevisionObject>
{
	/// <summary>
	/// The number of properties that a new instance makes room for.
	/// </summary>
	const int CAPACITY = 3;
	/// <summary>
	/// The type's full name, which <see cref="SetPSTypeName"/> puts first in the PowerShell type names.
	/// </summary>
	static readonly string _typeName = typeof(RevisionObject).GetName();
	/// <summary>
	/// Whether a constructor set the instance's properties, rather than the JSON converter.
	/// </summary>
	readonly bool _wasCtored;

	/// <summary>
	/// Gets a value that indicates whether the release is a repack.
	/// </summary>
	/// <value>The value of the <c>IsRepack</c> note property, or <see langword="false"/> when the object has none.</value>
	public bool IsRepack
	{
		get => this.GetValue<bool>();
		private set => this.ReplaceStructProperty(nameof(this.IsRepack), value, isReadOnly: true);
	}
	/// <summary>
	/// Gets or sets the release's real count, which is 1 or more for a real release.
	/// </summary>
	/// <remarks>
	/// Setting the property replaces the <c>Real</c> note property with a read-only one that holds the new value.
	/// </remarks>
	/// <value>The value of the <c>Real</c> note property, or 0 when the object has none.</value>
	public int Real
	{
		get => this.GetValue<int>();
		set => this.ReplaceNumberProperty(nameof(this.Real), value, isReadOnly: true);
	}
	/// <summary>
	/// Gets or sets the revision's version, which is 2 or more for a proper.
	/// </summary>
	/// <remarks>
	/// Setting the property replaces the <c>Version</c> note property with a read-only one that holds the new value.
	/// </remarks>
	/// <value>The value of the <c>Version</c> note property, or 0 when the object has none.</value>
	public int Version
	{
		get => this.GetValue<int>();
		set => this.ReplaceNumberProperty(nameof(this.Version), value, isReadOnly: true);
	}

	/// <summary>
	/// Initializes a new <see cref="RevisionObject"/> instance with no properties, which the JSON converter fills.
	/// </summary>
	public RevisionObject()
		: base(CAPACITY)
	{
	}
	/// <summary>
	/// Initializes a new <see cref="RevisionObject"/> instance from the proper and real flags.
	/// </summary>
	/// <remarks>
	/// The revision isn't a repack, its <see cref="Version"/> is 2 for a proper and 1 otherwise, and its
	/// <see cref="Real"/> is 1 for a real release and 0 otherwise.
	/// </remarks>
	/// <param name="isProper"><see langword="true"/> if the release is a proper; otherwise, <see langword="false"/>.</param>
	/// <param name="isReal"><see langword="true"/> if the release is a real one; otherwise, <see langword="false"/>.</param>
	private RevisionObject(bool isProper, bool isReal)
		: this()
	{
		_wasCtored = true;
		this.IsRepack = false;
		this.Version = isProper ? 2 : 1;
		this.Real = isReal ? 1 : 0;
	}

	//BUG: CompareTo compares only whether each instance was built in C#, so every two revisions read from JSON compare as equal whatever their Version and Real.
	/// <summary>
	/// Compares this revision with another.
	/// </summary>
	/// <param name="other">The revision to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this revision sorts before <paramref name="other"/>, 0 if they sort together, or a
	/// positive number if it sorts after <paramref name="other"/> or <paramref name="other"/> is
	/// <see langword="null"/>.
	/// </returns>
	public int CompareTo(RevisionObject? other)
	{
		return _wasCtored.CompareTo(other?._wasCtored);
	}
	/// <summary>
	/// Gets the quality revision tag from a resolver.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which this method ignores.</param>
	/// <returns>The resolver's <see cref="Meta.REVISION"/> tag.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.REVISION];
	}
	/// <summary>
	/// Puts the type's full name first in the object's PowerShell type names, ahead of the name that the base class
	/// adds.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// Creates a revision from the proper and real flags.
	/// </summary>
	/// <param name="isProper">
	/// <see langword="true"/> if the release is a proper, which gives the revision a <see cref="Version"/> of 2;
	/// otherwise, <see langword="false"/>.
	/// </param>
	/// <param name="isReal">
	/// <see langword="true"/> if the release is a real one, which gives the revision a <see cref="Real"/> of 1;
	/// otherwise, <see langword="false"/>.
	/// </param>
	/// <returns>
	/// A new revision that isn't a repack, which is <see cref="Default"/> when neither flag is set.
	/// </returns>
	public static RevisionObject Create(bool isProper, bool isReal)
	{
		if (!isProper && !isReal)
		{
			return Default;
		}

		return new(isProper, isReal);
	}
	/// <summary>
	/// Gets a new revision for a release that's neither a proper, a real, nor a repack.
	/// </summary>
	/// <value>
	/// A new instance on each read, with a <see cref="Version"/> of 1, a <see cref="Real"/> of 0, and an
	/// <see cref="IsRepack"/> of <see langword="false"/>.
	/// </value>
	public static RevisionObject Default => new(false, false);
}

