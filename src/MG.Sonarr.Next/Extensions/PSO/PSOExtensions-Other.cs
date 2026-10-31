using System.Management.Automation;

namespace MG.Sonarr.Next.Extensions.PSO;

public static partial class PSOExtensions
{
	[DebuggerStepThrough]
	public static T? GetValue<T>(this PSObject pso, [CallerMemberName] string propertyName = "")
	{
		object? value = pso.Properties[propertyName]?.Value;
		return value switch
		{
			T tVal => tVal,
			null => default,
			_ when LanguagePrimitives.TryConvertTo(value, out T? convertedTo) && convertedTo is not null => convertedTo,
			_ => default,
		};
	}

	/// <summary>
	/// Gets the value of the property as a string or an empty string if the value is null or cannot be converted to a string.
	/// </summary>
	/// <param name="pso">The <see cref="PSObject"/> this method is extending.</param>
	/// <param name="propertyName">The name of the property to get the value of.</param>
	/// <returns>The value of the property as a string or an empty string if the value is null or cannot be converted to a string.</returns>
	[DebuggerStepThrough]
	public static string GetStringOrEmpty(this PSObject pso, [CallerMemberName] string propertyName = "")
	{
		object? value = pso.Properties[propertyName]?.Value;
		return value switch
		{
			string strValue => strValue,
			null => string.Empty,
			_ when LanguagePrimitives.TryConvertTo(value, out string? convertedTo) && convertedTo is not null => convertedTo,
			_ => string.Empty,
		};
	}

	[DebuggerStepThrough]
	public static void SetValue<T>(this PSObject pso, T? value, [CallerMemberName] string propertyName = "")
	{
		ArgumentNullException.ThrowIfNull(pso);

		PSPropertyInfo? prop = pso.Properties[propertyName];
		if (prop is null)
		{
			pso.AddProperty(propertyName, value);
			return;
		}

		prop.Value = value;
	}

	[return: NotNullIfNotNull(nameof(pso))]
	public static Dictionary<string, object?>? ToDictionary(this PSObject? pso, params ReadOnlySpan<string> ignoreKeys)
	{
		if (pso is null)
			return null;

		Dictionary<string, object?> dic = new(StringComparer.OrdinalIgnoreCase);
		foreach (PSPropertyInfo prop in pso.Properties)
		{
			if (ignoreKeys.Contains(prop.Name))
			{
				continue;
			}

			if (prop.Value is null)
			{
				dic.TryAdd(prop.Name, prop.Value);
				continue;
			}

			object val = prop.Value switch
			{
				PSObject innerPso => ToDictionary(innerPso, ignoreKeys),
				string str => str,
				IEnumerable collection => ProcessCollection(collection, ignoreKeys),
				_ => prop.Value
			};

			dic.TryAdd(prop.Name, val);
		}

		return dic;
	}

	private static List<object?> ProcessCollection(IEnumerable collection, ReadOnlySpan<string> ignoreKeys)
	{
		List<object?> list = [];
		foreach (object? item in collection)
		{
			object? val = item switch
			{
				PSObject innerPso => ToDictionary(innerPso, ignoreKeys),
				string str => str,
				IEnumerable innerCollection => ProcessCollection(innerCollection, ignoreKeys),
				_ => item
			};

			list.Add(val);
		}
		return list;
	}

	/// <summary>
	/// Gets the value of the specified property as <typeparamref name="T"/> when the property exists and holds a value
	/// that isn't <see langword="null"/> and reads as that type.
	/// </summary>
	/// <inheritdoc cref="TryGetProperty{T}(PSObject, string, out T)" path="/remarks"/>
	/// <typeparam name="T">The type to read the value as.</typeparam>
	/// <param name="pso">The object whose property is read.</param>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="value">When this method returns <see langword="true"/>, the property's value.</param>
	/// <returns><see langword="true"/> if the property exists and holds a value that isn't <see langword="null"/> and
	/// reads as <typeparamref name="T"/>; otherwise, <see langword="false"/>.</returns>
	[DebuggerStepThrough]
	public static bool TryGetNonNullProperty<T>(this PSObject? pso, string propertyName, [NotNullWhen(true)] out T? value)
	{
		return TryGetProperty<T>(pso, propertyName, out value) && value is not null;
	}

	/// <summary>
	/// Gets the value of the specified property as <typeparamref name="T"/> when the property exists and its value reads
	/// as that type.
	/// </summary>
	/// <remarks>
	/// <para>A value of type <typeparamref name="T"/> reads as itself. The JSON converter reads a whole number as an
	/// <see cref="int"/> when it fits and as a <see cref="long"/> otherwise, so an <see cref="int"/> also reads as a
	/// <see cref="long"/>, and either one as a <see cref="double"/> or a <see cref="decimal"/>, or as the nullable form of
	/// any of them. No other value converts, and the read throws no exception.</para>
	/// <para>A <see langword="null"/> value reads only as a type that can hold it, so it fails for a value type that
	/// isn't nullable.</para>
	/// </remarks>
	/// <typeparam name="T">The type to read the value as.</typeparam>
	/// <param name="pso">The object whose property is read.</param>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="value">When this method returns <see langword="true"/>, the property's value; otherwise, the default
	/// value of <typeparamref name="T"/>.</param>
	/// <returns><see langword="true"/> if the property exists and its value reads as <typeparamref name="T"/>; otherwise,
	/// <see langword="false"/>.</returns>
	[DebuggerStepThrough]
	public static bool TryGetProperty<T>(this PSObject? pso, string propertyName, [MaybeNull] out T value)
	{
		value = default;
		PSMemberInfo? prop = pso?.Properties[propertyName];
		return prop is not null && TryRead(prop.Value, out value);
	}

	/// <summary>
	/// Reads a property's value as <typeparamref name="T"/>, widening the integers that the JSON converter produces.
	/// </summary>
	/// <typeparam name="T">The type to read the value as.</typeparam>
	/// <param name="obj">The property's value.</param>
	/// <param name="tVal">When this method returns <see langword="true"/>, the value as <typeparamref name="T"/>;
	/// otherwise, the default value of <typeparamref name="T"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="obj"/> reads as <typeparamref name="T"/>; otherwise,
	/// <see langword="false"/>.</returns>
	private static bool TryRead<T>(object? obj, [MaybeNull] out T tVal)
	{
		switch (obj)
		{
			case T value:
				tVal = value;
				return true;

			case null:
				tVal = default;
				return default(T) is null;

			case int intValue:
				return TryWiden(intValue, out tVal);

			case long longValue:
				return TryWiden(longValue, out tVal);

			default:
				tVal = default;
				return false;
		}
	}

	/// <summary>
	/// Widens an integer to <typeparamref name="T"/> when <typeparamref name="T"/> is <see cref="long"/>,
	/// <see cref="double"/>, or <see cref="decimal"/>, or the nullable form of one.
	/// </summary>
	/// <typeparam name="T">The type to widen the integer to.</typeparam>
	/// <param name="number">The integer, which is an <see cref="int"/> or a <see cref="long"/> before it's passed.</param>
	/// <param name="tVal">When this method returns <see langword="true"/>, the widened value; otherwise, the default value
	/// of <typeparamref name="T"/>.</param>
	/// <returns><see langword="true"/> if <typeparamref name="T"/> is one of the wider types; otherwise,
	/// <see langword="false"/>.</returns>
	private static bool TryWiden<T>(long number, [MaybeNull] out T tVal)
	{
		if (typeof(T) == typeof(long) || typeof(T) == typeof(long?))
		{
			tVal = (T)(object)number;
		}
		else if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
		{
			tVal = (T)(object)(double)number;
		}
		else if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
		{
			tVal = (T)(object)(decimal)number;
		}
		else
		{
			tVal = default;
			return false;
		}

		return true;
	}
}
