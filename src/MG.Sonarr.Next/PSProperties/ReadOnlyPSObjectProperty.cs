using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties;

/// <summary>
/// Represents a read-only note property that holds a nested <see cref="PSObject"/>.
/// </summary>
/// <remarks>
/// The property can't be replaced, though the nested object's own properties can change when they're writable.
/// </remarks>
/// <typeparam name="T">The type of the nested object.</typeparam>
public sealed class ReadOnlyPSObjectProperty<T> : ReadOnlyProperty<T> where T : PSObject
{
	/// <summary>
	/// Gets the nested object that the property holds.
	/// </summary>
	/// <value>The property's value.</value>
	protected override T ValueAsT { get; }

	/// <summary>
	/// Initializes a new <see cref="ReadOnlyPSObjectProperty{T}"/> instance with the specified name and nested object.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="pso">The nested object that the property holds.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is empty.</exception>
	public ReadOnlyPSObjectProperty(string propertyName, T pso)
		: base(propertyName)
	{
		this.ValueAsT = pso;
	}

	/// <summary>
	/// Creates a copy of the property that holds a copy of the nested object.
	/// </summary>
	/// <returns>
	/// A new <see cref="ReadOnlyPSObjectProperty{T}"/> with the same name, holding the result of the nested object's
	/// <see cref="PSObject.Copy"/>.
	/// </returns>
	/// <exception cref="InvalidCastException">
	/// Thrown when the nested object's <see cref="PSObject.Copy"/> returns an object that isn't a
	/// <typeparamref name="T"/>.
	/// </exception>
	public override PSMemberInfo Copy()
	{
		return new ReadOnlyPSObjectProperty<T>(this.Name, (T)this.ValueAsT.Copy());
	}
}

