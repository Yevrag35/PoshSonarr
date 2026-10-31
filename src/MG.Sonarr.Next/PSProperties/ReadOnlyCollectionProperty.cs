using System.Collections.Immutable;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties;

/// <summary>
/// Represents a read-only note property that holds an immutable array.
/// </summary>
/// <remarks>
/// The array can't change, though its elements can when they're mutable objects.
/// </remarks>
/// <typeparam name="T">The type of the array's elements.</typeparam>
public sealed class ReadOnlyCollectionProperty<T> : ReadOnlyProperty<ImmutableArray<T>>
{
	/// <summary>
	/// Gets the array that the property holds.
	/// </summary>
	/// <value>The property's value.</value>
	public ImmutableArray<T> Collection { get; }
	/// <summary>
	/// Gets the property's value as an <see cref="ImmutableArray{T}"/>.
	/// </summary>
	/// <value>The value of <see cref="Collection"/>.</value>
	protected override ImmutableArray<T> ValueAsT => this.Collection;

	/// <summary>
	/// Initializes a new <see cref="ReadOnlyCollectionProperty{T}"/> instance with the specified name and array.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="list">The array that the property holds.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is empty.</exception>
	public ReadOnlyCollectionProperty(string propertyName, ImmutableArray<T> list)
		: base(propertyName)
	{
		this.Collection = list;
	}

	/// <summary>
	/// Returns the property itself as its copy.
	/// </summary>
	/// <remarks>
	/// Neither the property nor its array can change, so a copy of the object that holds the property shares this
	/// instance with the original.
	/// </remarks>
	/// <returns>The current instance.</returns>
	public override PSMemberInfo Copy()
	{
		return this;
	}
}

/// <summary>
/// Represents a read-only note property that holds a collection, which the property can't replace but code can
/// change.
/// </summary>
/// <typeparam name="T">The type of the collection's elements.</typeparam>
/// <typeparam name="TCol">The type of the collection.</typeparam>
public sealed class ReadOnlyCollectionProperty<T, TCol> : ReadOnlyProperty<TCol>
	where TCol : IEnumerable<T>, new()
{
	/// <summary>
	/// Gets the collection that the property holds.
	/// </summary>
	/// <value>The property's value.</value>
	public TCol Collection { get; }
	/// <summary>
	/// Gets the property's value as a <typeparamref name="TCol"/>.
	/// </summary>
	/// <value>The value of <see cref="Collection"/>.</value>
	protected override TCol ValueAsT => this.Collection;

	/// <summary>
	/// Initializes a new <see cref="ReadOnlyCollectionProperty{T, TCol}"/> instance with the specified name and
	/// collection.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="collection">
	/// The collection that the property holds, or <see langword="null"/> for a new, empty one.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is empty.</exception>
	public ReadOnlyCollectionProperty(string propertyName, TCol? collection)
		: base(propertyName)
	{
		this.Collection = collection ?? [];
	}

	/// <summary>
	/// Returns the property itself as its copy.
	/// </summary>
	/// <returns>The current instance.</returns>
	public override PSMemberInfo Copy()
	{
		return this;
	}
}

