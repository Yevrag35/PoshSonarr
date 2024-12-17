using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.PSProperties;
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

            foreach (var exp in expressions)
            {
                if (!exp.TryGetAsMember()
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