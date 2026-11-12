using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.PSProperties;
using MG.Sonarr.Next.Reflection;

namespace MG.Sonarr.Next.Extensions.Pwsh;

static partial class PSOExtensions
{
	/// <summary>
	/// Extension methods for adding properties to <see cref="PSObject"/> instances.
	/// </summary>
	/// <param name="pso">The <see cref="PSObject"/> to which the properties will be added.</param>
	extension(PSObject pso)
	{
		/// <summary>
		/// Adds a numeric note property with the specified name and value.
		/// </summary>
		/// <typeparam name="T">The numeric value type.</typeparam>
		/// <param name="propertyName">The property name.</param>
		/// <param name="value">The numeric value.</param>
		/// <exception cref="System.ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
		internal void AddNumberProperty<T>(string propertyName, T value) where T : unmanaged, INumber<T>
		{
			ArgumentException.ThrowIfNullOrEmpty(propertyName);

			pso.Properties.Add(new NumberNoteProperty<T>(propertyName, value));
		}

		/// <summary>
		/// Adds a writable property with a type selected from the runtime value.
		/// </summary>
		/// <param name="propertyName">The property name.</param>
		/// <param name="value">The value to assign.</param>
		/// <exception cref="System.ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
		internal void AddProperty(string propertyName, object? value)
		{
			ArgumentException.ThrowIfNullOrEmpty(propertyName);
			switch (value)
			{
				case int intValue:
					pso.Properties.Add(NumberProperty.Create(propertyName, intValue));
					break;

				case long longValue:
					pso.Properties.Add(NumberProperty.Create(propertyName, longValue));
					break;

				case double dubValue:
					pso.Properties.Add(NumberProperty.Create(propertyName, dubValue));
					break;

				case string strValue:
					pso.Properties.Add(new StringNoteProperty(propertyName, strValue));
					break;

				case MetadataTag tag:
					pso.Properties.Add(new MetadataProperty(tag));
					break;

				default:
					pso.Properties.Add(new PSNoteProperty(propertyName, value));
					break;
			}
		}
		/// <summary>
		/// Adds a read-only property with the specified name and value to the given <see cref="PSObject"/> instance.
		/// </summary>
		/// <remarks>The property added is read-only and cannot be modified after creation. The method
		/// selects an appropriate property type based on the value's type, supporting common types such as <see
		/// langword="int"/>, <see langword="long"/>, <see langword="double"/>, <see langword="string"/>, and <see
		/// cref="MetadataTag"/>. For other types, and for <see langword="null"/>, a generic read-only property is used. A
		/// <see cref="MetadataTag"/> value always becomes the object's <c>MetadataTag</c> property, whatever
		/// <paramref name="propertyName"/> says.</remarks>
		/// <param name="propertyName">The name of the property to add. Cannot be null or empty.</param>
		/// <param name="value">The value to assign to the property. The property's type will be determined based on the runtime type of
		/// this value.</param>
		internal void AddReadOnlyProperty(string propertyName, object? value)
		{
			ArgumentException.ThrowIfNullOrEmpty(propertyName);
			switch (value)
			{
				case int intValue:
					pso.Properties.Add(ReadOnlyNumberProperty.Create(propertyName, intValue));
					break;

				case long longValue:
					pso.Properties.Add(ReadOnlyNumberProperty.Create(propertyName, longValue));
					break;

				case double dubValue:
					pso.Properties.Add(ReadOnlyNumberProperty.Create(propertyName, dubValue));
					break;

				case string strValue:
					pso.Properties.Add(new ReadOnlyStringProperty(propertyName, strValue));
					break;

				case MetadataTag tag:
					pso.Properties.Add(new MetadataProperty(tag));
					break;

				default:
					pso.Properties.Add(new ReadOnlyProperty(propertyName, value));
					break;
			}
		}
		/// <summary>
		/// Replaces the specified property of the PSObject with a read-only string property if conversion is possible.
		/// </summary>
		/// <remarks>If the property specified by propertyName can be converted to a read-only string
		/// property, it is removed and replaced. If conversion is not possible, no changes are made.</remarks>
		/// <param name="propertyName">The name of the property to replace with a read-only string property. Cannot be null.</param>
		internal void ReplaceWithReadOnlyStringProperty(string propertyName)
		{
			if (pso.Properties[propertyName].TryConvertToReadOnly(out ReadOnlyStringProperty? rosp))
			{
				pso.Properties.Remove(propertyName);
				pso.Properties.Add(rosp);
			}
		}
		/// <summary>
		/// Replaces the specified property with a read-only numeric property of type <typeparamref name="T"/>.
		/// </summary>
		/// <remarks>
		/// A string value is parsed with the invariant culture, as Sonarr writes it, and any other value that isn't a
		/// <typeparamref name="T"/> is converted by PowerShell's own conversion, which reads it the same way.
		/// </remarks>
		/// <typeparam name="T">The numeric value type.</typeparam>
		/// <param name="propertyName">The property name.</param>
		internal void ReplaceWithReadOnlyNumberProperty<T>(string propertyName) where T : unmanaged, INumber<T>
		{
			PSPropertyInfo? property = pso.Properties[propertyName];
			if (property is null || property is ReadOnlyNumberProperty<T>)
				return;

			ReadOnlyNumberProperty<T> newProp;
			switch (property.Value)
			{
				case T numValue:
					newProp = ReadOnlyNumberProperty.Create(propertyName, numValue);
					break;

				case string strValue when T.TryParse(strValue, CultureInfo.InvariantCulture, out T result):
					newProp = ReadOnlyNumberProperty.Create(propertyName, result);
					break;

				default:
					newProp = ReadOnlyNumberProperty.Create(propertyName, LanguagePrimitives.ConvertTo<T>(property.Value));
					break;
			}

			pso.Properties.Remove(propertyName);
			pso.Properties.Add(newProp);
		}

		/// <summary>
		/// Replaces the specified property with a read-only struct property of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The struct value type.</typeparam>
		/// <param name="propertyName">The property name.</param>
		internal void ReplaceWithReadOnlyStructProperty<T>(string propertyName) where T : struct
		{
			PSPropertyInfo? property = pso.Properties[propertyName];
			if (property is null || property is ReadOnlyStructProperty<T>)
				return;

			ReadOnlyStructProperty<T> newProp;
			switch (property.Value)
			{
				case T numValue:
					newProp = ReadOnlyStructProperty.Create(propertyName, numValue);
					break;

				default:
					newProp = ReadOnlyStructProperty.Create(propertyName, LanguagePrimitives.ConvertTo<T>(property.Value));
					break;
			}

			pso.Properties.Remove(propertyName);
			pso.Properties.Add(newProp);
		}

		/// <summary>
		/// Sets the specified number property to a value, adding the property when the object doesn't have it.
		/// </summary>
		/// <remarks>
		/// A writable property of another kind is replaced with a number property. A read-only property makes the method
		/// throw unless <paramref name="replaceReadOnly"/> is <see langword="true"/>, in which case it's replaced with a
		/// read-only number property, so a property that PowerShell can't change stays that way.
		/// </remarks>
		/// <typeparam name="T">The numeric type of the value.</typeparam>
		/// <param name="number">The value to set.</param>
		/// <param name="replaceReadOnly"><see langword="true"/> to replace a read-only property with a read-only one that
		/// holds <paramref name="number"/>; otherwise, <see langword="false"/>.</param>
		/// <param name="propertyName">The name of the property, which defaults to the calling member's name.</param>
		/// <exception cref="ReadOnlyPropertyException">Thrown when the property is read-only and
		/// <paramref name="replaceReadOnly"/> is false.</exception>
		internal void UpdateProperty<T>(T number, bool replaceReadOnly = false, [CallerMemberName] string propertyName = "")
			where T : unmanaged, INumber<T>
		{
			PSPropertyInfo? rawProperty = pso.Properties[propertyName];
			if (rawProperty is NumberNoteProperty<T> numberProp)
			{
				numberProp.NumValue = number;
				return;
			}

			if (rawProperty is not null && !rawProperty.IsSettable)
			{
				if (!replaceReadOnly)
				{
					throw new ReadOnlyPropertyException(propertyName);
				}

				pso.Properties.Remove(propertyName);
				pso.Properties.Add(ReadOnlyNumberProperty.Create(propertyName, number));
				return;
			}

			pso.Properties.Remove(propertyName);
			pso.Properties.Add(NumberProperty.Create(propertyName, number));
		}

		/// <summary>
		/// Updates the specified property value, adding the property when it does not exist.
		/// </summary>
		/// <param name="value">The value to assign.</param>
		/// <param name="replaceReadOnly"><see langword="true"/> to replace a read-only property; otherwise, <see langword="false"/>.</param>
		/// <param name="propertyName">The property name, which defaults to the calling member's name.</param>
		/// <exception cref="ReadOnlyPropertyException">Thrown when the property is read-only and <paramref name="replaceReadOnly"/> is false.</exception>
		internal void UpdateProperty(object? value, bool replaceReadOnly = false, [CallerMemberName] string propertyName = "")
		{
			PSPropertyInfo? rawProperty = pso.Properties[propertyName];
			if (rawProperty is null)
			{
				AddProperty(pso, propertyName, value);
				return;
			}

			if (!rawProperty.IsSettable)
			{
				if (!replaceReadOnly)
				{
					throw new ReadOnlyPropertyException(propertyName);
				}

				pso.Properties.Remove(propertyName);
				AddReadOnlyProperty(pso, propertyName, value);
				return;
			}

			rawProperty.Value = value;
		}
	}

	/// <summary>
	/// Replaces the specified numeric property with a writable numeric property.
	/// </summary>
	/// <typeparam name="T">The Sonarr object type.</typeparam>
	/// <typeparam name="TValue">The numeric value type.</typeparam>
	/// <param name="pso">The object whose property is replaced.</param>
	/// <param name="propertyName">The property name.</param>
	/// <param name="value">The replacement value.</param>
	/// <exception cref="System.ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
	internal static void ReplaceNumberProperty<T, TValue>(this T pso, string propertyName, TValue value)
		where T : SonarrObject
		where TValue : unmanaged, INumber<TValue>
	{
		ReplaceNumberProperty(pso, propertyName, value, isReadOnly: false);
	}

	/// <summary>
	/// Replaces the specified numeric property with a writable or read-only numeric property.
	/// </summary>
	/// <typeparam name="T">The Sonarr object type.</typeparam>
	/// <typeparam name="TValue">The numeric value type.</typeparam>
	/// <param name="pso">The object whose property is replaced.</param>
	/// <param name="propertyName">The property name.</param>
	/// <param name="value">The replacement value.</param>
	/// <param name="isReadOnly"><see langword="true"/> to add a read-only property; otherwise, <see langword="false"/>.</param>
	/// <exception cref="System.ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
	internal static void ReplaceNumberProperty<T, TValue>(this T pso, string propertyName, TValue value, bool isReadOnly)
		where T : SonarrObject
		where TValue : unmanaged, INumber<TValue>
	{
		ArgumentException.ThrowIfNullOrEmpty(propertyName);

		pso.Properties.Remove(propertyName);
		PSPropertyInfo info = isReadOnly
			? new ReadOnlyNumberProperty<TValue>(propertyName, value)
			: new NumberNoteProperty<TValue>(propertyName, value);

		pso.Properties.Add(info);
	}

	/// <summary>
	/// Replaces the specified struct property with a writable struct property.
	/// </summary>
	/// <typeparam name="T">The Sonarr object type.</typeparam>
	/// <typeparam name="TValue">The struct value type.</typeparam>
	/// <param name="pso">The object whose property is replaced.</param>
	/// <param name="propertyName">The property name.</param>
	/// <param name="value">The replacement value.</param>
	/// <exception cref="System.ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="pso"/> is null.</exception>
	internal static void ReplaceStructProperty<T, TValue>(this T pso, string propertyName, TValue value)
		where T : SonarrObject
		where TValue : struct
	{
		ReplaceStructProperty(pso, propertyName, value, isReadOnly: false);
	}

	/// <summary>
	/// Replaces the specified struct property with a writable or read-only struct property.
	/// </summary>
	/// <typeparam name="T">The Sonarr object type.</typeparam>
	/// <typeparam name="TValue">The struct value type.</typeparam>
	/// <param name="pso">The object whose property is replaced.</param>
	/// <param name="propertyName">The property name.</param>
	/// <param name="value">The replacement value.</param>
	/// <param name="isReadOnly"><see langword="true"/> to add a read-only property; otherwise, <see langword="false"/>.</param>
	/// <exception cref="System.ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="pso"/> is null.</exception>
	internal static void ReplaceStructProperty<T, TValue>(this T pso, string propertyName, TValue value, bool isReadOnly)
		where T : SonarrObject
		where TValue : struct
	{
		ArgumentNullException.ThrowIfNull(pso);
		ArgumentException.ThrowIfNullOrEmpty(propertyName);

		pso.Properties.Remove(propertyName);
		PSPropertyInfo info = isReadOnly
			? new ReadOnlyStructProperty<TValue>(propertyName, value)
			: new StructNoteProperty<TValue>(propertyName, value);

		pso.Properties.Add(info);
	}

	/// <summary>
	/// Attempts to convert the specified property to a read-only string property.
	/// </summary>
	/// <remarks>If the property is already a <see cref="ReadOnlyStringProperty"/>, the method returns
	/// false and does not perform a conversion. The conversion uses the property's value, formatting it as a string
	/// with the invariant culture when possible, since the value can go back to Sonarr.</remarks>
	/// <param name="property">The property to convert. Must not be null.</param>
	/// <param name="result">When this method returns <see langword="true"/>, contains the resulting <see cref="ReadOnlyStringProperty"/>
	/// instance. When it returns <see langword="false"/>, contains <paramref name="property"/> itself if it's already a
	/// <see cref="ReadOnlyStringProperty"/>, and <see langword="null"/> otherwise.</param>
	/// <returns>true if the conversion was successful and <paramref name="result"/> contains a valid <see
	/// cref="ReadOnlyStringProperty"/>; otherwise, false.</returns>
	internal static bool TryConvertToReadOnly([NotNullWhen(true)] this PSPropertyInfo? property, [NotNullWhen(true)] out ReadOnlyStringProperty? result)
	{
		if (property is null)
		{
			result = null;
			return false;
		}

		if (property is ReadOnlyStringProperty rosp)
		{
			result = rosp;
			return false;
		}

		object? value = property.Value;
		result = value switch
		{
			null => new ReadOnlyStringProperty(property.Name, value: null),
			string s => new ReadOnlyStringProperty(property.Name, s),
			IFormattable f => new ReadOnlyStringProperty(property.Name, f.ToString(format: null, CultureInfo.InvariantCulture)),
			IConvertible c => new ReadOnlyStringProperty(property.Name, c.ToString(CultureInfo.InvariantCulture)),
			_ => new ReadOnlyStringProperty(property.Name, value.ToString()),
		};

		return true;
	}

	/// <summary>
	/// Updates multiple properties on a Sonarr object by using member expressions.
	/// </summary>
	/// <typeparam name="T">The Sonarr object type.</typeparam>
	/// <param name="pso">The object whose properties are updated.</param>
	/// <param name="expressions">The member expressions that identify properties to update from current values.</param>
	[DebuggerStepThrough]
	internal static void UpdateProperty<T>(this T pso, params ReadOnlySpan<Expression<Func<T, object?>>> expressions)
		where T : SonarrObject
	{
		UpdateProperty(pso, replaceReadOnly: false, expressions);
	}

	/// <summary>
	/// Updates multiple properties on a Sonarr object by using member expressions.
	/// </summary>
	/// <typeparam name="T">The Sonarr object type.</typeparam>
	/// <param name="pso">The object whose properties are updated.</param>
	/// <param name="replaceReadOnly"><see langword="true"/> to replace read-only properties; otherwise, <see langword="false"/>.</param>
	/// <param name="expressions">The member expressions that identify properties to update from current values.</param>
	/// <exception cref="System.InvalidOperationException">Thrown when an expression does not represent a readable member.</exception>
	internal static void UpdateProperty<T>(this T pso, bool replaceReadOnly, params ReadOnlySpan<Expression<Func<T, object?>>> expressions)
		where T : SonarrObject
	{
		foreach (var exp in expressions)
		{
			FieldOrPropertyInfo info = exp.GetMemberInfo();
			if (info.IsEmpty || !info.CanGet)
			{
				throw new InvalidOperationException("The expression does not represent a readable member.");
			}

			UpdateProperty(pso, info.GetValue(pso), replaceReadOnly, info.MemberName);
		}
	}
}