using System.Management.Automation;

namespace MG.Sonarr.Next.Extensions.PSO;

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
	/// 
	/// </summary>
	/// <typeparam name="TProp"></typeparam>
	/// <param name="pso"></param>
	/// <param name="propertyName"></param>
	/// <param name="propertyFactory"></param>
	/// <returns></returns>
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
	/// 
	/// </summary>
	/// <typeparam name="TProp"></typeparam>
	/// <typeparam name="TState"></typeparam>
	/// <param name="pso"></param>
	/// <param name="propertyName"></param>
	/// <param name="state"></param>
	/// <param name="propertyFactory"></param>
	/// <returns></returns>
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