using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.PSProperties;
using MG.Sonarr.Next.Reflection;
using Newtonsoft.Json.Linq;
using System.Management.Automation;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Extensions.PSO
{
    public static partial class PSOExtensions
    {
        public static void AddNumberProperty<T>(this PSObject pso, string propertyName, T value) where T : unmanaged, INumber<T>
        {
            ArgumentException.ThrowIfNullOrEmpty(propertyName);

            pso.Properties.Add(new NumberNoteProperty<T>(propertyName, value));
        }

        public static void AddProperty(this PSObject pso, string propertyName, object? value)
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
        /// cref="MetadataTag"/>. For other types, a generic note property is used.</remarks>
        /// <param name="pso">The <see cref="PSObject"/> to which the property will be added.</param>
        /// <param name="propertyName">The name of the property to add. Cannot be null or empty.</param>
        /// <param name="value">The value to assign to the property. The property's type will be determined based on the runtime type of
        /// this value.</param>
        public static void AddReadOnlyProperty(this PSObject pso, string propertyName, object? value)
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
                    pso.Properties.Add(new PSNoteProperty(propertyName, value));
                    break;
            }
        }
        /// <summary>
        /// Replaces the specified property of the PSObject with a read-only string property if conversion is possible.
        /// </summary>
        /// <remarks>If the property specified by propertyName can be converted to a read-only string
        /// property, it is removed and replaced. If conversion is not possible, no changes are made.</remarks>
        /// <param name="pso">The PSObject whose property will be replaced.</param>
        /// <param name="propertyName">The name of the property to replace with a read-only string property. Cannot be null.</param>
        public static void ReplaceWithReadOnlyStringProperty(this PSObject pso, string propertyName)
        {
            if (pso.Properties[propertyName].TryConvertToReadOnly(out ReadOnlyStringProperty? rosp))
            {
                pso.Properties.Remove(propertyName);
                pso.Properties.Add(rosp);
            }
        }
        public static void ReplaceWithReadOnlyNumberProperty<T>(this PSObject pso, string propertyName) where T : unmanaged, INumber<T>
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

                case string strValue when T.TryParse(strValue, CultureInfo.CurrentCulture, out T result):
                    newProp = ReadOnlyNumberProperty.Create(propertyName, result);
                    break;

                default:
                    newProp = ReadOnlyNumberProperty.Create(propertyName, LanguagePrimitives.ConvertTo<T>(property.Value));
                    break;
            }

            pso.Properties.Remove(propertyName);
            pso.Properties.Add(newProp);
        }
        public static void ReplaceWithReadOnlyStructProperty<T>(this PSObject pso, string propertyName) where T : struct
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

        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public static void ReplaceNumberProperty<T, TValue>(this T pso, string propertyName, TValue value)
            where T : SonarrObject
            where TValue : unmanaged, INumber<TValue>
        {
            ReplaceNumberProperty(pso, propertyName, value, isReadOnly: false);
        }
        public static void ReplaceNumberProperty<T, TValue>(this T pso, string propertyName, TValue value, bool isReadOnly)
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
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public static void ReplaceStructProperty<T, TValue>(this T pso, string propertyName, TValue value)
            where T : SonarrObject
            where TValue : struct
        {
            ReplaceStructProperty(pso, propertyName, value, isReadOnly: false);
        }
        public static void ReplaceStructProperty<T, TValue>(this T pso, string propertyName, TValue value, bool isReadOnly)
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
        /// when possible.</remarks>
        /// <param name="property">The property to convert. Must not be null.</param>
        /// <param name="result">When this method returns <see langword="true"/>, contains the resulting <see cref="ReadOnlyStringProperty"/>
        /// instance; otherwise, <see langword="null"/>.</param>
        /// <returns>true if the conversion was successful and <paramref name="result"/> contains a valid <see
        /// cref="ReadOnlyStringProperty"/>; otherwise, false.</returns>
        public static bool TryConvertToReadOnly([NotNullWhen(true)] this PSPropertyInfo? property, [NotNullWhen(true)] out ReadOnlyStringProperty? result)
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
                IFormattable f => new ReadOnlyStringProperty(property.Name, f.ToString(format: null, CultureInfo.CurrentCulture)),
                IConvertible c => new ReadOnlyStringProperty(property.Name, c.ToString(CultureInfo.CurrentCulture)),
                _ => new ReadOnlyStringProperty(property.Name, value.ToString()),
            };

            return true;
        }

        public static void UpdateProperty<T>(this PSObject pso, T number, bool replaceReadOnly = false, [CallerMemberName] string propertyName = "")
            where T : unmanaged, INumber<T>
        {
            PSPropertyInfo? rawProperty = pso.Properties[propertyName];
            if (rawProperty is null || rawProperty is not NumberNoteProperty<T> numberProp)
            {
                pso.Properties.Remove(propertyName);
                pso.Properties.Add(NumberProperty.Create(propertyName, number));

                return;
            }

            if (!numberProp.IsSettable)
            {
                if (!replaceReadOnly)
                {
                    throw new ReadOnlyPropertyException(propertyName);
                }

                pso.Properties.Remove(propertyName);
                pso.Properties.Add(ReadOnlyNumberProperty.Create(propertyName, number));
                return;
            }

            numberProp.NumValue = number;
        }

        [DebuggerStepThrough]
        public static void UpdateProperty<T>(this T pso, params ReadOnlySpan<Expression<Func<T, object?>>> expressions)
            where T : SonarrObject
        {
            UpdateProperty(pso, replaceReadOnly: false, expressions);
        }
        public static void UpdateProperty<T>(this T pso, bool replaceReadOnly, params ReadOnlySpan<Expression<Func<T, object?>>> expressions)
            where T : SonarrObject
        {
            if (expressions.IsEmpty)
            {
                return;
            }

            foreach (ref readonly var exp in expressions)
            {
                FieldOrPropertyInfo info = exp.GetMemberInfo();
                if (info.IsEmpty || !info.CanGet)
                {
                    throw new InvalidOperationException("The expression does not represent a readable member.");
                }

                UpdateProperty(pso, info.GetValue(pso), replaceReadOnly, info.MemberName);
            }
        }
        public static void UpdateProperty(this PSObject pso, object? value, bool replaceReadOnly = false, [CallerMemberName] string propertyName = "")
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

        ///// <exception cref="ArgumentNullException"/>
        ///// <exception cref="ReadOnlyPropertyException"/>
        //[Obsolete("Stop using", error: true)]
        //public static void UpdateProperty<T>(this T pso, Expression<Func<T, object?>> expression) where T : SonarrObject
        //{
        //    ArgumentNullException.ThrowIfNull(expression);

        //    if (!expression.TryGetAsMember(out var memberExp))
        //    {
        //        return;
        //    }

        //    var func = expression.Compile();
        //    UpdateProperty(pso, memberExp.Member.Name, func(pso));
        //}

        ///// <exception cref="ArgumentException"/>
        ///// <exception cref="ArgumentNullException"/>
        ///// <exception cref="ReadOnlyPropertyException">
        //[Obsolete("Stop using", error: true)]
        //public static void UpdateProperty<T>(this T pso, string propertyName, object? value) where T : PSObject
        //{
        //    ArgumentNullException.ThrowIfNull(pso);
        //    ArgumentException.ThrowIfNullOrEmpty(propertyName);

        //    PSPropertyInfo? propInfo = pso.Properties[propertyName];
        //    if (propInfo is null)
        //    {
        //        propInfo = WritableProperty.ToProperty<T>(propertyName, value);
        //        pso.Properties.Add(propInfo);
        //    }
        //    else if (propInfo is ReadOnlyProperty)
        //    {
        //        throw new ReadOnlyPropertyException(propertyName);
        //    }
        //    else if (propInfo is WritableProperty writable && writable.ValueIsProper(value))
        //    {
        //        writable.Value = value;
        //    }
        //    else
        //    {
        //        propInfo.Value = value;
        //    }
        //}


    }
}