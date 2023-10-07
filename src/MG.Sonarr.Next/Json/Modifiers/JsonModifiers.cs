using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json.Attributes;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace MG.Sonarr.Next.Json.Modifiers
{
    public static class JsonModifiers
    {
        public static void AddPrivateFieldsModifier(JsonTypeInfo typeInfo)
        {
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

        private static string GetAdjustedFieldName(string fieldName)
        {
            Span<char> scratch = stackalloc char[fieldName.Length];
            fieldName.CopyTo(scratch);
            bool changed = false;
            char underscore = '_';

            if (scratch.StartsWith(in underscore))
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
                c = char.ToUpper(c);
                changed = true;
            }

            return changed ? new string(scratch) : fieldName;
        }
        private static string GetJsonName(FieldInfo fieldInfo, JsonTypeInfo typeInfo, Type jsonNameType)
        {
            if (!fieldInfo.IsDefined(jsonNameType, inherit: false))
            {
                return GetNonUnderscoreName(fieldInfo.Name, typeInfo);
            }

            CustomAttributeTypedArgument arg = fieldInfo.CustomAttributes
                .First(x => x.AttributeType == jsonNameType)
                    .ConstructorArguments
                        .FirstOrDefault();

            return arg.Value as string ?? string.Empty;
        }
        private static string GetNonUnderscoreName(string? fieldName, JsonTypeInfo typeInfo)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return string.Empty;
            }

            fieldName = GetAdjustedFieldName(fieldName);

            return typeInfo.Options.PropertyNamingPolicy?.ConvertName(fieldName) ?? fieldName;
        }
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
}