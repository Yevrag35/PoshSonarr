namespace MG.Sonarr.Next.Shell.Components;

/// <summary>
/// Represents a property of an object that <c>Get-SonarrObjectProperty</c> writes: its name, the type of its value, its
/// value, and whether it can be set.
/// </summary>
/// <remarks>
/// <para>
/// Properties sort by <see cref="ObjectType"/> and then by <see cref="Name"/>, compared ordinally and ignoring case, so
/// a sorted set keeps one property for each name of each object type.
/// </para>
/// <para>
/// Equality is the record's own, which compares all five properties, <see cref="CurrentValue"/> through its own
/// <see cref="object.Equals(object)"/>. Two properties that <see cref="CompareTo(SonarrProperty)"/> puts in the same
/// place can still be unequal.
/// </para>
/// </remarks>
public sealed record SonarrProperty : IComparable<SonarrProperty>
{
	/// <summary>
	/// Gets the property's value.
	/// </summary>
	/// <value>The value that the property had when the cmdlet read it, or <see langword="null"/>.</value>
	public required object? CurrentValue { get; init; }
	/// <summary>
	/// Gets a value that indicates whether the property can't be set.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the property is read-only; otherwise, <see langword="false"/>. The default is
	/// <see langword="false"/>.
	/// </value>
	public bool IsReadOnly { get; init; }
	/// <summary>
	/// Gets the property's name.
	/// </summary>
	/// <value>The name of the property.</value>
	public required string Name { get; init; }
	/// <summary>
	/// Gets the type of the object that has the property.
	/// </summary>
	/// <value>
	/// The PowerShell name of the object's .NET type, without its enclosing brackets, such as a model class's full name.
	/// </value>
	public required string ObjectType { get; init; }
	/// <summary>
	/// Gets the type of the property's value.
	/// </summary>
	/// <value>The name of the type that PowerShell gives for the property's value.</value>
	public required string Type { get; init; }

	/// <summary>
	/// Compares the property with another by the type of their objects and then by their names.
	/// </summary>
	/// <param name="other">The property to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this property comes first, 0 if both have the same object type and name, ignoring case, and
	/// a positive number if it comes after <paramref name="other"/> or <paramref name="other"/> is
	/// <see langword="null"/>.
	/// </returns>
	public int CompareTo(SonarrProperty? other)
	{
		int compare = string.Compare(this.ObjectType, other?.ObjectType, StringComparison.OrdinalIgnoreCase);
		if (compare == 0)
		{
			compare = string.Compare(this.Name, other?.Name, StringComparison.OrdinalIgnoreCase);
		}

		return compare;
	}
}

