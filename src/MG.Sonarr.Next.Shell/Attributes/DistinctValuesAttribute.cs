using MG.Sonarr.Next.Shell.Checkers;
using System.Collections;

namespace MG.Sonarr.Next.Shell.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class DistinctValuesAttribute : EnumerableTransformAttribute
{
    private static readonly Dictionary<Type, EqualityChecker> _checkers = [];

    public Type? CollectionType
    {
        get => this.CollectionTypeCore;
        init => this.CollectionTypeCore = value;
    }

    public DistinctValuesAttribute(Type elementType) : base(elementType)
    {
    }

    protected override Array TransformCore([DisallowNull] IEnumerable<object> input, Type elementType, Type inputType, EngineIntrinsics engineIntrinsics, IServiceProvider provider)
    {
        if (!_checkers.TryGetValue(elementType, out EqualityChecker? checker))
        {
            checker = CreateChecker(elementType);
            _checkers.TryAdd(elementType, checker);
        }

        IEnumerable<object> converted = input.Select(x => LanguagePrimitives.ConvertTo(x, elementType, Statics.DefaultProvider));

        return converted.Distinct(checker).ToArray();
    }

    private static bool IsEquatable(Type type)
    {
        Type def = typeof(IEquatable<>).MakeGenericType(type);

        return def.IsAssignableFrom(type);
    }

    private static EqualityChecker CreateChecker(Type elementType)
    {
        if (!IsEquatable(elementType))
        {
            return EqualityChecker.Default;
        }

        return (EqualityChecker)Activator.CreateInstance(typeof(EqualityChecker<>).MakeGenericType(elementType))!;
    }

}