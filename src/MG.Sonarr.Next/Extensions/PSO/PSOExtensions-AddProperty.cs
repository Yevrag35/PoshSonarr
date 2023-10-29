using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.PSProperties;
using System.Management.Automation;
using System.Numerics;

namespace MG.Sonarr.Next.Extensions.PSO
{
    public static partial class PSOExtensions
    {
        public static void AddNumberProperty<T>(this PSObject pso, string propertyName, T value) where T : unmanaged, INumber<T>
        {
            ArgumentNullException.ThrowIfNull(pso);
            ArgumentException.ThrowIfNullOrEmpty(propertyName);

            pso.Properties.Add(new NumberNoteProperty<T>(propertyName, value));
        }

        public static void AddProperty<T>(this PSObject pso, string propertyName, T value)
        {
            ArgumentNullException.ThrowIfNull(pso);
            ArgumentException.ThrowIfNullOrEmpty(propertyName);

            pso.Properties.Add(new PSNoteProperty(propertyName, value));
        }
        public static void AddProperty<T, TProp>(this T pso, Expression<Func<T, TProp>> expression)
            where T : SonarrObject
        {
            ArgumentNullException.ThrowIfNull(pso);
            ArgumentNullException.ThrowIfNull(expression);

            if (!expression.TryGetAsMember(out var member))
            {
                return;
            }

            var func = expression.Compile();
            pso.Properties.Add(new PSNoteProperty(member.Member.Name, func(pso)));
        }
        public static void AddProperties<T>(this T? pso, params Expression<Func<T, object?>>[] expressions)
            where T : SonarrObject
        {
            if (pso is null || expressions is null || expressions.Length <= 0)
            {
                return;
            }

            foreach (var exp in expressions)
            {
                AddProperty(pso, exp);
            }
        }

        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public static void ReplaceNumberProperty<T, TValue>(this T pso, string propertyName, TValue value)
            where T : SonarrObject
            where TValue : unmanaged, INumber<TValue>
        {
            ArgumentNullException.ThrowIfNull(pso);
            ArgumentException.ThrowIfNullOrEmpty(propertyName);

            pso.Properties.Remove(propertyName);
            pso.Properties.Add(new NumberNoteProperty<TValue>(propertyName, value));
        }
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public static void ReplaceStructProperty<T, TValue>(this T pso, string propertyName, TValue value)
            where T : SonarrObject
            where TValue : struct
        {
            ArgumentNullException.ThrowIfNull(pso);
            ArgumentException.ThrowIfNullOrEmpty(propertyName);

            pso.Properties.Remove(propertyName);
            pso.Properties.Add(new StructNoteProperty<TValue>(propertyName, value));
        }

        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ReadOnlyPropertyException"/>
        public static void UpdateProperty<T>(this T pso, Expression<Func<T, object?>> expression) where T : SonarrObject
        {
            ArgumentNullException.ThrowIfNull(expression);

            if (!expression.TryGetAsMember(out var memberExp))
            {
                return;
            }

            var func = expression.Compile();
            UpdateProperty(pso, memberExp.Member.Name, func(pso));
        }

        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ReadOnlyPropertyException">
        public static void UpdateProperty<T>(this T pso, string propertyName, object? value) where T : PSObject
        {
            ArgumentNullException.ThrowIfNull(pso);
            ArgumentException.ThrowIfNullOrEmpty(propertyName);

            PSPropertyInfo? propInfo = pso.Properties[propertyName];
            if (propInfo is null)
            {
                propInfo = WritableProperty.ToProperty<T>(propertyName, value);
                pso.Properties.Add(propInfo);
            }
            else if (propInfo is ReadOnlyProperty)
            {
                throw new ReadOnlyPropertyException(propertyName);
            }
            else if (propInfo is WritableProperty writable && writable.ValueIsProper(value))
            {
                writable.Value = value;
            }
            else
            {
                propInfo.Value = value;
            }
        }
    }
}