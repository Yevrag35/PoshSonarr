using MG.Sonarr.Next.Extensions.Reflection;

namespace MG.Sonarr.Next.PSProperties;

/// <summary>
/// Represents a read-only note property that holds a sorted set, which the property can't replace but code can
/// change.
/// </summary>
/// <typeparam name="T">The type of the set's elements.</typeparam>
public sealed class ReadOnlySetProperty<T> : ReadOnlyProperty<SortedSet<T>>
{
	/// <summary>
	/// The format of the start of <see cref="ToString"/>'s result, with the element type's name and the property's name.
	/// </summary>
	private const string AS_STRING = "read-only SortedSet{0} {1}=";
	/// <summary>
	/// The format of <see cref="ToString"/>'s result for a set with elements, which it lists in braces.
	/// </summary>
	private const string AS_VALUES = AS_STRING + "{{{2}}}";
	/// <summary>
	/// The format of <see cref="ToString"/>'s result for an empty set.
	/// </summary>
	private const string AS_EMPTY = AS_STRING + "{{}}";
	/// <summary>
	/// The PowerShell name of <typeparamref name="T"/>, in brackets.
	/// </summary>
	private string? _genName;

	/// <summary>
	/// Gets the set that the property holds.
	/// </summary>
	/// <value>The property's value.</value>
	public SortedSet<T> SetValues { get; internal set; }
	/// <summary>
	/// Gets the property's value as a <see cref="SortedSet{T}"/>.
	/// </summary>
	/// <value>The value of <see cref="SetValues"/>.</value>
	protected override SortedSet<T> ValueAsT => this.SetValues;

	/// <summary>
	/// Initializes a new <see cref="ReadOnlySetProperty{T}"/> instance with the specified name and set.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="set">The set that the property holds.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is empty.</exception>
	public ReadOnlySetProperty(string propertyName, SortedSet<T> set)
		: base(propertyName)
	{
		this.SetValues = set;
	}

	/// <summary>
	/// Creates a copy of the property that holds a copy of its set.
	/// </summary>
	/// <remarks>
	/// The copy's set holds the same elements in a new set of the same runtime type: a <see cref="SortedSet{T}"/> with
	/// the same comparer, or an instance of a derived set, made through its public parameterless constructor, which
	/// sets its comparer. A change to either set doesn't show in the other.
	/// </remarks>
	/// <returns>A new <see cref="ReadOnlySetProperty{T}"/> with the same name that holds a copy of the set.</returns>
	/// <exception cref="MissingMethodException">
	/// Thrown when the set's runtime type derives from <see cref="SortedSet{T}"/> and has no public parameterless
	/// constructor.
	/// </exception>
	public override PSMemberInfo Copy()
	{
		return new ReadOnlySetProperty<T>(this.Name, CopySet(this.SetValues));
	}

	/// <summary>
	/// Creates a set of the same runtime type that holds the same elements.
	/// </summary>
	/// <param name="set">The set to copy.</param>
	/// <returns>A new set that holds the elements of <paramref name="set"/>.</returns>
	/// <exception cref="MissingMethodException">
	/// Thrown when the runtime type of <paramref name="set"/> derives from <see cref="SortedSet{T}"/> and has no public
	/// parameterless constructor.
	/// </exception>
	private static SortedSet<T> CopySet(SortedSet<T> set)
	{
		Type setType = set.GetType();
		if (setType == typeof(SortedSet<T>))
		{
			return new SortedSet<T>(set, set.Comparer);
		}

		var copy = (SortedSet<T>)Activator.CreateInstance(setType)!;
		copy.UnionWith(set);
		return copy;
	}

	/// <summary>
	/// Returns a description of the property, with its element type, name, and elements.
	/// </summary>
	/// <returns>
	/// The text <c>read-only SortedSet[&lt;type&gt;] &lt;name&gt;={&lt;elements&gt;}</c>, with the elements separated
	/// by commas, such as <c>read-only SortedSet[string] Required={a,b}</c>.
	/// </returns>
	public override string ToString()
	{
		_genName ??= typeof(T).GetPSTypeName();
		return this.SetValues.Count > 0
			? string.Format(CultureInfo.CurrentCulture, AS_VALUES, _genName, this.Name, string.Join(',', this.SetValues))
			: string.Format(CultureInfo.CurrentCulture, AS_EMPTY, _genName, this.Name);
	}
}

