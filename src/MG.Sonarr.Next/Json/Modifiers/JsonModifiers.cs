using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Json.Attributes;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace MG.Sonarr.Next.Json.Modifiers;

/// <summary>
/// Provides JSON type-info modifiers used by Sonarr serialization configuration.
/// </summary>
public static class JsonModifiers
{
	/// <summary>
	/// Adds private fields declared by <see cref="JsonIncludePrivateFieldsAttribute"/> to object type metadata.
	/// </summary>
	/// <param name="typeInfo">The JSON type metadata to modify.</param>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="typeInfo"/> is null.</exception>
	public static void AddPrivateFieldsModifier(JsonTypeInfo typeInfo)
	{
		ArgumentNullException.ThrowIfNull(typeInfo);

		if (typeInfo.Kind != JsonTypeInfoKind.Object)
		{
			return;
		}

		Type attType = typeof(JsonIncludePrivateFieldsAttribute);
		Type jsonNameType = typeof(JsonPropertyNameAttribute);

		if (!typeInfo.Type.IsDefined(attType, inherit: false))
		{
			return;
		}

		CustomAttributeTypedArgument ctor = typeInfo.Type.CustomAttributes
			.First(x => x.AttributeType == attType)
				.ConstructorArguments
					.FirstOrDefault();

		if (ctor.Value is not ReadOnlyCollection<CustomAttributeTypedArgument> col)
		{
			return;
		}

		foreach (CustomAttributeTypedArgument arg in col)
		{
			AddFieldToTypeInfo(typeInfo, jsonNameType, in arg);
		}
	}

	/// <summary>
	/// Adds a reflected field to the specified JSON type metadata as a serializable property.
	/// </summary>
	/// <param name="typeInfo">The target JSON type metadata.</param>
	/// <param name="jsonNameType">The <see cref="JsonPropertyNameAttribute"/> type reference.</param>
	/// <param name="argument">The attribute argument containing the private field name.</param>
	private static void AddFieldToTypeInfo(JsonTypeInfo typeInfo, Type jsonNameType, in CustomAttributeTypedArgument argument)
	{
		if (argument.Value is not string fieldName)
		{
			return;
		}

		if (!TryGetField(typeInfo, fieldName, out FieldInfo? fi))
		{
			return;
		}

		var propInfo = typeInfo.CreateJsonPropertyInfo(
			propertyType: fi.FieldType,
			name: GetJsonName(fi, typeInfo, jsonNameType));

		propInfo.Get = fi.GetValue;
		propInfo.Set = fi.SetValue;

		typeInfo.Properties.Add(propInfo);
	}

	/// <summary>
	/// Normalizes a field name by trimming leading underscores and capitalizing the first character.
	/// </summary>
	/// <param name="fieldName">The field name to normalize.</param>
	/// <returns>The normalized field name.</returns>
	private static string GetAdjustedFieldName(string fieldName)
	{
		Span<char> scratch = stackalloc char[fieldName.Length];
		fieldName.CopyTo(scratch);
		bool changed = false;
		char underscore = '_';

		if (scratch.StartsWith(underscore))
		{
			int index = scratch.IndexOfAnyExcept(underscore);
			scratch = scratch.Slice(index);
			if (scratch.IsEmpty)
			{
				return fieldName;
			}

			changed = true;
		}

		ref char c = ref scratch[0];
		if (!char.IsUpper(c))
		{
			c = char.ToUpperInvariant(c);
			changed = true;
		}

		return changed ? new string(scratch) : fieldName;
	}

	/// <summary>
	/// Gets the JSON property name for a reflected member.
	/// </summary>
	/// <param name="memberInfo">The member metadata.</param>
	/// <param name="typeInfo">The target JSON type metadata.</param>
	/// <param name="jsonNameType">The <see cref="JsonPropertyNameAttribute"/> type reference.</param>
	/// <returns>The JSON property name.</returns>
	private static string GetJsonName(MemberInfo memberInfo, JsonTypeInfo typeInfo, Type jsonNameType)
	{
		if (!memberInfo.IsDefined(jsonNameType, inherit: false))
		{
			return GetNonUnderscoreName(memberInfo.Name, typeInfo);
		}

		CustomAttributeTypedArgument arg = memberInfo.CustomAttributes
			.First(x => x.AttributeType == jsonNameType)
				.ConstructorArguments
					.FirstOrDefault();

		return arg.Value as string ?? string.Empty;
	}

	/// <summary>
	/// Gets a field name adjusted for naming policy conversion.
	/// </summary>
	/// <param name="fieldName">The source field name.</param>
	/// <param name="typeInfo">The target JSON type metadata.</param>
	/// <returns>The adjusted JSON property name, or an empty string when <paramref name="fieldName"/> is blank.</returns>
	private static string GetNonUnderscoreName(string? fieldName, JsonTypeInfo typeInfo)
	{
		if (string.IsNullOrWhiteSpace(fieldName))
		{
			return string.Empty;
		}

		fieldName = GetAdjustedFieldName(fieldName);

		return typeInfo.Options.PropertyNamingPolicy?.ConvertName(fieldName) ?? fieldName;
	}

	/// <summary>
	/// Tries to resolve a non-public instance field by name.
	/// </summary>
	/// <param name="info">The JSON type metadata whose runtime type is searched.</param>
	/// <param name="fieldName">The private field name.</param>
	/// <param name="field">The resolved field when found; otherwise, <see langword="null"/>.</param>
	/// <returns><see langword="true"/> when the field is resolved; otherwise, <see langword="false"/>.</returns>
	private static bool TryGetField(JsonTypeInfo info, string fieldName, [NotNullWhen(true)] out FieldInfo? field)
	{
		field = null;
		if (string.IsNullOrWhiteSpace(fieldName))
		{
			return false;
		}

		try
		{
			field = info.Type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
		}
		catch (Exception e)
		{
			Debug.WriteLine(e);
			return false;
		}

		return field is not null;
	}
}