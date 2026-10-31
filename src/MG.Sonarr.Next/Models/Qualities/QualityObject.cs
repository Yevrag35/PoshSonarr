using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.Qualities;

/// <summary>
/// Represents one quality, a resolution and source that Sonarr names, such as <c>HDTV-1080p</c>.
/// </summary>
/// <remarks>
/// <para>
/// Sonarr sends a quality nested in a quality definition or a quality revision. Its <c>Id</c>, <c>Name</c>,
/// <c>Resolution</c>, and <c>Source</c> properties are read-only, and qualities sort by ID.
/// </para>
/// <para>
/// A quality that a constructor builds holds the properties that it was built with, and
/// <see cref="OnDeserialized"/> leaves it as it is. <see cref="Default"/> is the quality that Sonarr calls
/// <c>Unknown</c>.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class QualityObject : SonarrObject,
	IComparable<QualityObject>,
	ISerializableNames<QualityObject>
{
	/// <summary>
	/// The number of properties that a new instance makes room for.
	/// </summary>
	const int CAPACITY = 4;
	/// <summary>
	/// The type's full name, which <see cref="SetPSTypeName"/> puts first in the PowerShell type names.
	/// </summary>
	static readonly string _typeName = typeof(QualityObject).GetName();
	/// <summary>
	/// Whether a constructor set the instance's properties, rather than the JSON converter.
	/// </summary>
	readonly bool _wasCtored;

	/// <summary>
	/// Gets the quality's ID.
	/// </summary>
	/// <value>
	/// The ID that Sonarr assigned, copied when the object is deserialized or set by the constructor that built it;
	/// otherwise, 0.
	/// </value>
	public int Id { get; private set; }
	/// <summary>
	/// Gets the quality's name.
	/// </summary>
	/// <value>The value of the <c>Name</c> note property, or an empty string when the object has none.</value>
	public string Name => this.GetStringOrEmpty();
	/// <summary>
	/// Gets the quality's vertical resolution, such as 1080.
	/// </summary>
	/// <value>The value of the <c>Resolution</c> note property, or 0 when the object has none.</value>
	public int Resolution => this.GetValue<int>();
	/// <summary>
	/// Gets the quality's source, such as <c>television</c> or <c>bluray</c>.
	/// </summary>
	/// <value>The value of the <c>Source</c> note property, or an empty string when the object has none.</value>
	public string Source => this.GetStringOrEmpty();

	/// <summary>
	/// Initializes a new <see cref="QualityObject"/> instance with no properties, which the JSON converter fills.
	/// </summary>
	public QualityObject()
		: base(CAPACITY)
	{
	}
	/// <summary>
	/// Initializes a new <see cref="QualityObject"/> instance with a copy of the quality of a quality revision.
	/// </summary>
	/// <param name="qualityAndRevision">The quality revision whose quality is copied.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the quality's ID or resolution is negative.
	/// </exception>
	/// <exception cref="ArgumentException">Thrown when the quality's name or source is empty.</exception>
	public QualityObject(QualityRevisionObject qualityAndRevision)
		: this(qualityAndRevision.Quality)
	{
	}
	/// <summary>
	/// Initializes a new <see cref="QualityObject"/> instance with a copy of another quality's ID, name, resolution,
	/// and source.
	/// </summary>
	/// <param name="copyFrom">The quality to copy.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the ID or resolution of <paramref name="copyFrom"/> is negative.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// Thrown when the name or source of <paramref name="copyFrom"/> is empty.
	/// </exception>
	private QualityObject(QualityObject copyFrom)
		: this(copyFrom.Id, copyFrom.Name, copyFrom.Resolution, copyFrom.Source)
	{
	}
	/// <summary>
	/// Initializes a new <see cref="QualityObject"/> instance with an ID, name, resolution, and source, each held in
	/// a read-only property.
	/// </summary>
	/// <param name="id">The quality's ID.</param>
	/// <param name="name">The quality's name.</param>
	/// <param name="resolution">The quality's vertical resolution.</param>
	/// <param name="source">The quality's source.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="id"/> or <paramref name="resolution"/> is negative.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="name"/> or <paramref name="source"/> is null.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="name"/> or <paramref name="source"/> is empty.
	/// </exception>
	private QualityObject(int id, string name, int resolution, string source)
		: this()
	{
		_wasCtored = true;

		ValidateNumber(id);
		ValidateNumber(resolution);
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentException.ThrowIfNullOrEmpty(source);

		this.Id = id;
		this.Properties.Add(new ReadOnlyNumberProperty<int>(nameof(this.Id), id));
		this.Properties.Add(new ReadOnlyStringProperty(nameof(this.Name), name));
		this.Properties.Add(new ReadOnlyNumberProperty<int>(nameof(this.Resolution), resolution));
		this.Properties.Add(new ReadOnlyStringProperty(nameof(this.Source), source));
	}

	/// <summary>
	/// Compares this quality with another by ID.
	/// </summary>
	/// <param name="other">The quality to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this quality's <see cref="Id"/> is less than that of <paramref name="other"/>, 0 if
	/// they're equal, or a positive number if it's greater or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public int CompareTo(QualityObject? other)
	{
		return Comparer<int?>.Default.Compare(this.Id, other?.Id);
	}
	/// <summary>
	/// Gets the quality tag from a resolver.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which this method ignores.</param>
	/// <returns>The resolver's <see cref="Meta.QUALITY"/> tag.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.QUALITY];
	}
	/// <summary>
	/// Finishes the object after the JSON converter reads it, copying its ID and making its name, resolution, and
	/// source read-only.
	/// </summary>
	/// <remarks>
	/// The method does nothing for an instance that a constructor built. Otherwise, the base class's steps run first.
	/// </remarks>
	public override void OnDeserialized()
	{
		if (_wasCtored)
		{
			return;
		}

		base.OnDeserialized();

		if (this.TryGetId(out int id))
		{
			this.Id = id;
		}

		ReplaceWithReadOnly(this.Properties, nameof(this.Name));
		ReplaceWithReadOnly(this.Properties, nameof(this.Resolution));
		ReplaceWithReadOnly(this.Properties, nameof(this.Source));
	}
	/// <summary>
	/// Replaces a writable property with a read-only one that has the same name and value.
	/// </summary>
	/// <remarks>
	/// A property that's missing, or that isn't a <see cref="WritableProperty"/>, is left as it is.
	/// </remarks>
	/// <param name="collection">The object's properties.</param>
	/// <param name="propertyName">The name of the property to replace.</param>
	private static void ReplaceWithReadOnly(PSMemberInfoCollection<PSPropertyInfo> collection, string propertyName)
	{
		PSPropertyInfo? prop = collection[propertyName];
		if (prop is WritableProperty writable)
		{
			collection.Remove(propertyName);
			collection.Add(writable.ConvertToReadOnly());
		}
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
	/// Throws when a number is negative.
	/// </summary>
	/// <param name="number">The number to check.</param>
	/// <param name="argumentName">
	/// The expression passed as <paramref name="number"/>, which the exception names as its parameter.
	/// </param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="number"/> is negative.</exception>
	private static void ValidateNumber(int number, [CallerArgumentExpression(nameof(number))] string argumentName = "")
	{
		if (number < 0)
		{
			throw new ArgumentOutOfRangeException(argumentName, number, "The number must be non-negative.");
		}
	}

	/// <summary>
	/// The quality that Sonarr calls <c>Unknown</c>, with an ID and resolution of 0 and the source <c>unknown</c>.
	/// </summary>
	/// <remarks>
	/// Every caller shares this one instance.
	/// </remarks>
	public static readonly QualityObject Default = new(0, "Unknown", 0, "unknown");
}

