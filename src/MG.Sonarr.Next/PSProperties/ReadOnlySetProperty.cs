using MG.Sonarr.Next.Extensions.Reflection;
using System.Management.Automation;

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
	private readonly string _genName;

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
		_genName = typeof(T).GetPSTypeName();
	}

	/// <summary>
	/// Returns the property itself as its copy.
	/// </summary>
	/// <returns>The current instance.</returns>
	public override PSMemberInfo Copy()
	{
		return this;
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
		if (this.SetValues.Count <= 0)
		{
			return string.Format(CultureInfo.CurrentCulture, AS_EMPTY, _genName, this.Name);
		}

		return string.Format(CultureInfo.CurrentCulture, AS_VALUES, _genName, this.Name, string.Join(',', this.SetValues));
	}
}

