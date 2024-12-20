using MG.Sonarr.Next.Shell.Checkers;
using System.Collections;
using System.Collections.Concurrent;

namespace MG.Sonarr.Next.Shell.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class DistinctValuesAttribute : EnumerableTransformAttribute
{
    [MaybeNull]
    public Type CollectionType
    {
        get => this.CollectionTypeCore;
        init => this.CollectionTypeCore = value;
    }

    public DistinctValuesAttribute(Type elementType) : base(elementType)
    {
    }

    protected override Array TransformCore([DisallowNull] IEnumerable<object> input, Type elementType, Type inputType, EngineIntrinsics engineIntrinsics, IServiceProvider provider)
    {
        var checkers = provider.GetRequiredService<ConcurrentDictionary<Type, EqualityChecker>>();
        var checker = checkers.GetOrAdd(elementType, CreateChecker);

        IEnumerable<object> converted = input.Select(x => LanguagePrimitives.ConvertTo(x, elementType, Statics.DefaultProvider));

        return converted.Distinct(checker).ToArray();
    }

    private static bool IsEquatable(Type[] types, int index)
    {
        Type def = typeof(IEquatable<>).MakeGenericType(types);

        return def.IsAssignableFrom(types.AsSpan(index)[0]);
    }

    private static EqualityChecker CreateChecker(Type elementType)
    {
        Type[] parameters = [elementType];
        if (elementType.IsEnum)
        {
            return (EqualityChecker)Activator.CreateInstance(typeof(EnumEqualityChecker<>).MakeGenericType(parameters))!;
        }
        
        if (IsEquatable(parameters, 0))
        {
            return (EqualityChecker)Activator.CreateInstance(typeof(EqualityChecker<>).MakeGenericType(parameters))!;
        }

        return EqualityChecker.Default;
    }
}