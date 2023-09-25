﻿using MG.Sonarr.Next.Services.Models;
using System.Management.Automation;

namespace MG.Sonarr.Next.Services.Extensions.PSO
{
    public static partial class PSOExtensions
    {
        public static void AddProperty<T>(this object? obj, string propertyName, T value)
        {
            if (obj is not PSObject pso)
            {
                return;
            }

            pso.AddProperty(propertyName, value);
        }
        public static void AddProperty<T>(this PSObject pso, string propertyName, T value)
        {
            pso.Properties.Add(new PSNoteProperty(propertyName, value));
        }

        public static void AddProperty<T, TProp>(this T pso, Expression<Func<T, TProp>> expression)
            where T : SonarrObject
        {
            if (!expression.TryGetAsMember(out var member))
            {
                return;
            }

            var func = expression.Compile();
            pso.Properties.Add(new PSNoteProperty(member.Member.Name, func(pso)));
        }
        public static void AddProperties<T>(this T pso, params Expression<Func<T, object?>>[] expressions)
            where T : SonarrObject
        {
            if (expressions is null || expressions.Length <= 0)
            {
                return;
            }

            foreach (var exp in expressions)
            {
                AddProperty(pso, exp);
            }
        }
        public static void UpdateProperty<T>(this T pso, Expression<Func<T, object?>> expression) where T : SonarrObject
        {
            if (!expression.TryGetAsMember(out var memberExp))
            {
                return;
            }

            var func = expression.Compile();
            UpdateProperty(pso, memberExp.Member.Name, func(pso));
        }
        public static void UpdateProperty<T>(this T pso, string propertyName, object? value) where T : PSObject
        {
            PSPropertyInfo? propInfo = pso.Properties[propertyName];
            if (propInfo is null)
            {
                propInfo = new PSNoteProperty(propertyName, value);
                pso.Properties.Add(propInfo);
            }
            else
            {
                propInfo.Value = value;
            }
        }
    }
}