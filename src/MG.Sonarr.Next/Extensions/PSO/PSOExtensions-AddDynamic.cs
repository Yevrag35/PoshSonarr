using System.Management.Automation;

namespace MG.Sonarr.Next.Extensions.PSO;

public static partial class PSOExtensions
{
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TProp"></typeparam>
	/// <typeparam name="TState"></typeparam>
	/// <param name="pso"></param>
	/// <param name="propertyName"></param>
	/// <param name="replaceReadOnly"></param>
	/// <param name="state"></param>
	/// <param name="valueFactory"></param>
	/// <exception cref="ReadOnlyPropertyException"></exception>
	public static void AddOrUpdate<TProp, TValue>(this PSObject pso,
		string propertyName,
		bool replaceReadOnly,
		TValue value,
		Func<string, TValue, TProp> valueFactory)
			where TProp : PSPropertyInfo
	{
		PSPropertyInfo? rawProperty = pso.Properties[propertyName];
		if (rawProperty is null || rawProperty is not TProp alreadyProperty)
		{
			pso.Properties.Remove(propertyName);
			pso.Properties.Add(valueFactory(propertyName, value));
			return;
		}

		if (!alreadyProperty.IsSettable)
		{
			if (!replaceReadOnly)
			{
				throw new ReadOnlyPropertyException(propertyName);
			}

			pso.Properties.Remove(propertyName);
			pso.Properties.Add(valueFactory(propertyName, value));
			return;
		}

		alreadyProperty.Value = value;
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