using MG.Sonarr.Next.PSProperties;

namespace MG.Sonarr.Next.Extensions.Pwsh;

static partial class PSOExtensions
{
	/// <summary>
	/// Extension methods for reading and writing properties of <see cref="PSObject"/> instances.
	/// </summary>
	/// <param name="pso">The <see cref="PSObject"/> instance to extend.</param>
	extension(PSObject pso)
	{
		/// <summary>
		/// Gets the value of the specified property as <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type to read the value as.</typeparam>
		/// <param name="propertyName">The property name, which defaults to the calling member's name.</param>
		/// <returns>The converted value when available; otherwise, the default value of <typeparamref name="T"/>.</returns>
		[DebuggerStepThrough]
		internal T? GetValue<T>([CallerMemberName] string propertyName = "")
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
		/// <param name="propertyName">The name of the property to get the value of.</param>
		/// <returns>The value of the property as a string or an empty string if the value is null or cannot be converted to a string.</returns>
		[DebuggerStepThrough]
		internal string GetStringOrEmpty([CallerMemberName] string propertyName = "")
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

		/// <summary>
		/// Sets the specified property to the provided value.
		/// </summary>
		/// <typeparam name="T">The value type.</typeparam>
		/// <param name="value">The value to assign.</param>
		/// <param name="propertyName">The property name, which defaults to the calling member's name.</param>
		/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="pso"/> is null.</exception>
		[DebuggerStepThrough]
		internal void SetValue<T>(T? value, [CallerMemberName] string propertyName = "")
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

		/// <summary>
		/// Converts a <see cref="PSObject"/> to a dictionary representation.
		/// </summary>
		/// <param name="ignoreKeys">Property names to omit.</param>
		/// <returns>A dictionary representation of <paramref name="pso"/>.</returns>
		internal Dictionary<string, object?> ToDictionary(params ReadOnlySpan<string> ignoreKeys)
		{
			Dictionary<string, object?> dic = new(StringComparer.OrdinalIgnoreCase);
			foreach (PSPropertyInfo prop in pso.Properties)
			{
				if (ignoreKeys.Contains(prop.Name))
				{
					continue;
				}

				ref object? dicValue = ref CollectionsMarshal.GetValueRefOrAddDefault(dic, prop.Name, out bool exists);

				dicValue = prop.Value switch
				{
					PSObject innerPso => ToDictionary(innerPso, ignoreKeys),
					string str => str,
					IEnumerable collection => ProcessCollection(collection, ignoreKeys),
					null => null,
					_ => prop.Value,
				};
			}

			return dic;
		}

		/// <summary>
		/// Gets the value of the specified property as <typeparamref name="T"/> when the property exists and holds a value
		/// that isn't <see langword="null"/> and reads as that type.
		/// </summary>
		/// <inheritdoc cref="TryGetProperty{T}(PSObject, string, out T)" path="/remarks"/>
		/// <typeparam name="T">The type to read the value as.</typeparam>
		/// <param name="propertyName">The name of the property.</param>
		/// <param name="value">When this method returns <see langword="true"/>, the property's value.</param>
		/// <returns><see langword="true"/> if the property exists and holds a value that isn't <see langword="null"/> and
		/// reads as <typeparamref name="T"/>; otherwise, <see langword="false"/>.</returns>
		[DebuggerStepThrough]
		internal bool TryGetNonNullProperty<T>(string propertyName, [NotNullWhen(true)] out T? value)
		{
			return TryGetProperty(pso, propertyName, out value) && value is not null;
		}

		/// <summary>
		/// Gets the value of the specified property as <typeparamref name="T"/> when the property exists and its value reads
		/// as that type.
		/// </summary>
		/// <remarks>
		/// <para>A value reads only as a type that it already is, such as its own type or an interface that it implements,
		/// and the read converts nothing. A whole number, which the JSON converter reads as an <see cref="int"/>, or as a
		/// <see cref="long"/> when it doesn't fit an <see cref="int"/>, doesn't read as a <see cref="double"/>. The read
		/// throws no exception.</para>
		/// <para>A <see langword="null"/> value reads only as a type that can hold it, so it fails for a value type that
		/// isn't nullable.</para>
		/// <para>When <typeparamref name="T"/> is a value type, a property that holds a <typeparamref name="T"/>, such as a
		/// number or a date that the JSON converter read, gives up its value through the property's typed getter, so the
		/// read doesn't box it.</para>
		/// </remarks>
		/// <typeparam name="T">The type to read the value as.</typeparam>
		/// <param name="propertyName">The name of the property.</param>
		/// <param name="value">When this method returns <see langword="true"/>, the property's value; otherwise, the default
		/// value of <typeparamref name="T"/>.</param>
		/// <returns><see langword="true"/> if the property exists and its value reads as <typeparamref name="T"/>; otherwise,
		/// <see langword="false"/>.</returns>
		[DebuggerStepThrough]
		internal bool TryGetProperty<T>(string propertyName, [MaybeNull] out T value)
		{
			PSPropertyInfo? prop = pso?.Properties[propertyName];
			// A reference type isn't boxed, and in shared code each of these checks would cost a dictionary lookup.
			if (typeof(T).IsValueType)
			{
				switch (prop)
				{
					case WritableProperty<T> writable:
						value = writable.GetValue();
						return true;

					case ReadOnlyProperty<T> readOnly:
						value = readOnly.GetValue();
						return true;
				}
			}

			switch (prop?.Value)
			{
				case T read:
					value = read;
					return true;

				case null:
					value = default;
					return prop is not null && default(T) is null;

				default:
					value = default;
					return false;
			}
		}
	}

	/// <summary>
	/// Converts an enumerable value to a list representation for dictionary projection.
	/// </summary>
	/// <param name="collection">The collection to convert.</param>
	/// <param name="ignoreKeys">Property names to omit from nested objects.</param>
	/// <returns>A list representation of <paramref name="collection"/>.</returns>
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
				_ => item,
			};

			list.Add(val);
		}

		return list;
	}
}
