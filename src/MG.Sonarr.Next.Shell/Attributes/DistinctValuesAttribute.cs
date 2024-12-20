using MG.Sonarr.Next.Shell.Checkers;
using System.Collections;

namespace MG.Sonarr.Next.Shell.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class DistinctValuesAttribute : ArgumentTransformationAttribute
{
    private static readonly Dictionary<Type, EqualityChecker> _checkers = [];

    public required Type ElementType { get; init; }
    public Type? CheckerType { get; init; }

    public override object? Transform(EngineIntrinsics engineIntrinsics, object? inputData)
    {
        if (inputData is null || !LanguagePrimitives.IsObjectEnumerable(inputData))
        {
            return inputData;
        }

        var array = this.GetDistinct((IEnumerable)inputData);
        return array;
    }

    private IEnumerable GetDistinct(IEnumerable enumerable)
    {
        if (this.CheckerType is not null && _checkers.TryGetValue(this.CheckerType, out EqualityChecker? checker))
        {
            return enumerable.Cast<object>().Distinct(checker).ToArray();
        }
    }

    private static bool IsEquatable(Type type)
    {
        Type def = typeof(IEquatable<>).MakeGenericType(type);

        return def.IsAssignableFrom(type);
    }
}