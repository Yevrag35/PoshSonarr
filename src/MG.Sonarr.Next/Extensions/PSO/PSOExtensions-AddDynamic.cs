using System.Management.Automation;

namespace MG.Sonarr.Next.Extensions.PSO;

/// <summary>
/// Provides dynamic property add or update helpers for <see cref="PSObject"/> instances.
/// </summary>
public static partial class PSOExtensions
{
	/// <summary>
	/// Sets the specified property to a value, or adds a property that <paramref name="valueFactory"/> creates when the
	/// object doesn't have one that can take the value.
	/// </summary>
	/// <remarks>
	/// A writable <typeparamref name="TProp"/> takes the value. A writable property of another kind is replaced with
	/// the one that <paramref name="valueFactory"/> creates. A read-only property of any kind makes the method throw
	/// unless <paramref name="replaceReadOnly"/> is <see langword="true"/>, in which case it's replaced too.
	/// </remarks>
	/// <typeparam name="TProp">The type of property that <paramref name="valueFactory"/> creates.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <param name="pso">The object whose property is set.</param>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="replaceReadOnly"><see langword="true"/> to replace a read-only property; otherwise,
	/// <see langword="false"/>.</param>
	/// <param name="value">The value to set.</param>
	/// <param name="valueFactory">The function that creates the property from its name and value.</param>
	/// <exception cref="ReadOnlyPropertyException">Thrown when the property is read-only and
	/// <paramref name="replaceReadOnly"/> is false.</exception>
	public static void AddOrUpdate<TProp, TValue>(this PSObject pso,
		string propertyName,
		bool replaceReadOnly,
		TValue value,
		Func<string, TValue, TProp> valueFactory)
			where TProp : PSPropertyInfo
	{
		PSPropertyInfo? rawProperty = pso.Properties[propertyName];
		if (rawProperty is TProp alreadyProperty && alreadyProperty.IsSettable)
		{
			alreadyProperty.Value = value;
			return;
		}

		if (rawProperty is not null && !rawProperty.IsSettable && !replaceReadOnly)
		{
			throw new ReadOnlyPropertyException(propertyName);
		}

		pso.Properties.Remove(propertyName);
		pso.Properties.Add(valueFactory(propertyName, value));
	}

	/// <summary>
	/// Gets an existing property of the specified type or adds one when missing or mismatched.
	/// </summary>
	/// <typeparam name="TProp">The property type to retrieve or create.</typeparam>
	/// <param name="pso">The object whose property is retrieved.</param>
	/// <param name="propertyName">The property name.</param>
	/// <param name="propertyFactory">The factory that creates the property when needed.</param>
	/// <returns>The existing or newly created property.</returns>
	public static TProp GetOrAdd<TProp>(this PSObject pso,
		string propertyName,
		Func<string, TProp> propertyFactory)
			where TProp : PSPropertyInfo
	{
		PSPropertyInfo? rawProperty = pso.Properties[propertyName];

		TProp property;
		if (rawProperty is null || rawProperty is not TProp alreadyProperty)
		{
			pso.Properties.Remove(propertyName);
			property = propertyFactory(propertyName);
			pso.Properties.Add(property);
		}
		else
		{
			property = alreadyProperty;
		}

		return property;
	}
	/// <summary>
	/// Gets an existing property of the specified type or adds one when missing or mismatched.
	/// </summary>
	/// <typeparam name="TProp">The property type to retrieve or create.</typeparam>
	/// <typeparam name="TState">The state type passed to the property factory.</typeparam>
	/// <param name="pso">The object whose property is retrieved.</param>
	/// <param name="propertyName">The property name.</param>
	/// <param name="state">The state passed to <paramref name="propertyFactory"/>.</param>
	/// <param name="propertyFactory">The factory that creates the property when needed.</param>
	/// <returns>The existing or newly created property.</returns>
	public static TProp GetOrAdd<TProp, TState>(this PSObject pso,
		string propertyName,
		TState state,
		Func<string, TState, TProp> propertyFactory)
			where TProp : PSPropertyInfo
	{
		PSPropertyInfo? rawProperty = pso.Properties[propertyName];

		TProp property;
		if (rawProperty is null || rawProperty is not TProp alreadyProperty)
		{
			pso.Properties.Remove(propertyName);
			property = propertyFactory(propertyName, state);
			pso.Properties.Add(property);
		}
		else
		{
			property = alreadyProperty;
		}

		return property;
	}
}