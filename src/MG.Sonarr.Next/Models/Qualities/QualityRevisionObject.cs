using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.ManualImports;

namespace MG.Sonarr.Next.Models.Qualities;

/// <summary>
/// Represents a quality and the revision of a release in it, such as the quality of a file in a manual import.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="ManualImportObject"/>'s <c>Quality</c> property holds one and is writable, so a user can replace it
/// before the import is posted. The constructors that take a <see cref="QualityObject"/> build one in C# for that.
/// </para>
/// <para>
/// The object gets no <see cref="SonarrObject.MetadataTag"/>, and quality revisions sort by quality.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class QualityRevisionObject : SonarrObject,
	IComparable<QualityRevisionObject>,
	ISerializableNames<QualityRevisionObject>
{
	/// <summary>
	/// The number of properties that a new instance makes room for.
	/// </summary>
	const int CAPACITY = 2;
	/// <summary>
	/// The type's full name, which <see cref="SetPSTypeName"/> puts first in the PowerShell type names.
	/// </summary>
	static readonly string _typeName = typeof(QualityRevisionObject).GetName();

	/// <summary>
	/// Gets a value that indicates whether <see cref="SonarrObject.SetTag(IMetadataResolver)"/> leaves the object
	/// untagged.
	/// </summary>
	/// <value>Always <see langword="true"/>.</value>
	protected override bool DisregardMetadataTag => true;
	/// <summary>
	/// Gets the quality.
	/// </summary>
	/// <value>
	/// The value of the <c>Quality</c> note property, or <see cref="QualityObject.Default"/> when the object has none.
	/// </value>
	public QualityObject Quality
	{
		get => this.GetValue<QualityObject>() ?? QualityObject.Default;
		private set => this.UpdateProperty(value);
	}
	/// <summary>
	/// Gets the revision of the release.
	/// </summary>
	/// <value>
	/// The value of the <c>Revision</c> note property, or a new <see cref="RevisionObject.Default"/> when the object
	/// has none.
	/// </value>
	public RevisionObject Revision
	{
		get => this.GetValue<RevisionObject>() ?? RevisionObject.Default;
		private set => this.UpdateProperty(value);
	}

	/// <summary>
	/// Initializes a new <see cref="QualityRevisionObject"/> instance with no properties, which the JSON converter
	/// fills.
	/// </summary>
	public QualityRevisionObject()
		: base(CAPACITY)
	{
	}
	/// <summary>
	/// Initializes a new <see cref="QualityRevisionObject"/> instance with a quality and the default revision.
	/// </summary>
	/// <param name="quality">The quality.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="quality"/> is null.</exception>
	public QualityRevisionObject(QualityObject quality)
		: this(quality, null)
	{
	}
	/// <summary>
	/// Initializes a new <see cref="QualityRevisionObject"/> instance with a quality and a revision built from the
	/// proper and real flags.
	/// </summary>
	/// <param name="quality">The quality.</param>
	/// <param name="isProper">
	/// <see langword="true"/> if the release is a proper, which gives the revision a <see cref="RevisionObject.Version"/>
	/// of 2; otherwise, <see langword="false"/>.
	/// </param>
	/// <param name="isReal">
	/// <see langword="true"/> if the release is a real one, which gives the revision a <see cref="RevisionObject.Real"/>
	/// of 1; otherwise, <see langword="false"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="quality"/> is null.</exception>
	public QualityRevisionObject(QualityObject quality, bool isProper, bool isReal)
		: this(quality, RevisionObject.Create(isProper, isReal))
	{
	}
	/// <summary>
	/// Initializes a new <see cref="QualityRevisionObject"/> instance with a quality and a revision.
	/// </summary>
	/// <param name="quality">The quality.</param>
	/// <param name="revision">The revision, or <see langword="null"/> for <see cref="RevisionObject.Default"/>.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="quality"/> is null.</exception>
	private QualityRevisionObject(QualityObject quality, RevisionObject? revision)
		: this()
	{
		ArgumentNullException.ThrowIfNull(quality);
		this.Quality = quality;
		this.Revision = revision ?? RevisionObject.Default;
	}

	/// <summary>
	/// Compares this quality revision with another by quality.
	/// </summary>
	/// <param name="other">The quality revision to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// The result of comparing this object's <see cref="Quality"/> with that of <paramref name="other"/>, which is
	/// positive when <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public int CompareTo(QualityRevisionObject? other)
	{
		return this.Quality.CompareTo(other?.Quality);
	}
	/// <summary>
	/// Returns the existing tag, since a quality revision stands for no endpoint.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags, which this method ignores.</param>
	/// <param name="existing">The object's current tag.</param>
	/// <returns><paramref name="existing"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return existing;
	}
	/// <summary>
	/// Determines whether the quality revision becomes a read-only property of the object that it's nested in.
	/// </summary>
	/// <param name="propertyName">The name of the property that holds the quality revision in its parent.</param>
	/// <param name="parentType">The type of the parent object.</param>
	/// <returns>
	/// <see langword="false"/> for the <c>Quality</c> property of a <see cref="ManualImportObject"/>, which the user
	/// sets before the import is posted; otherwise, <see langword="true"/>.
	/// </returns>
	internal override bool ShouldBeReadOnly(string propertyName, Type parentType)
	{
		if (nameof(this.Quality) == propertyName && typeof(ManualImportObject).Equals(parentType))
		{
			return false;
		}

		return base.ShouldBeReadOnly(propertyName, parentType);
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
}

