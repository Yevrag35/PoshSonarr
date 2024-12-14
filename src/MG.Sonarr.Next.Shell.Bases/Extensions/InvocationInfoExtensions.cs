using System.Reflection;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Shell.Extensions;

public static class InvocationInfoExtensions
{
    private static bool _hasChecked;
    [MemberNotNullWhen(true, nameof(_positionallyBoundGetter))]
    private static bool CanCheck { get; set; }

    private static PropertyInfo? _listProperty;
    private static Func<object, IReadOnlyCollection<string>>? _positionallyBoundGetter;

    static InvocationInfoExtensions()
    {
        CanCheck = false;
        _hasChecked = false;
    }

    [MemberNotNullWhen(true, nameof(_positionallyBoundGetter))]
    public static bool CheckCanCheckPositionalBinding(Dictionary<string, object?> example)
    {
        if (_hasChecked)
        {
            return CanCheck;
        }

        bool canCheck = TryGetListProperty(example, out _positionallyBoundGetter);
        CanCheck = canCheck;
        _hasChecked = true;
        return canCheck;
    }

    [DebuggerStepThrough]
    public static bool IsBoundPositionally<T>(this InvocationInfo invocation, T value, [CallerArgumentExpression(nameof(value))] string parameterName = "") where T : struct
    {
        return IsBoundPositionally(invocation, parameterName);
    }
    public static bool IsBoundPositionally(this InvocationInfo invocation, string parameterName)
    {
        if (!CheckCanCheckPositionalBinding(invocation.BoundParameters))
        {
            return false;
        }

        return IsPositionallyBound(parameterName, _positionallyBoundGetter, invocation.BoundParameters);
    }

    private static bool IsPositionallyBound(string parameterName, Func<object, IReadOnlyCollection<string>> getter, Dictionary<string, object?> boundParameters)
    {
        IReadOnlyCollection<string> positionallyBound = getter(boundParameters);

        return positionallyBound.Count > 0
            && positionallyBound.Contains(parameterName, StringComparer.OrdinalIgnoreCase);
    }
    private static bool TryGetListProperty(Dictionary<string, object?> boundParameters, [NotNullWhen(true)] out Func<object, IReadOnlyCollection<string>>? getter)
    {
        Type type = boundParameters.GetType();
        string propertyName = "BoundPositionally";

        PropertyInfo? listProperty = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (listProperty is null)
        {
            getter = null;
            return false;
        }

        _listProperty = listProperty;
        getter = static (object o) =>
        {
            object? list = _listProperty.GetValue(o);
            return list is IReadOnlyCollection<string> iCol
                ? iCol
                : list is IEnumerable<string> iEnum
                    ? iEnum.ToArray()
                    : Array.Empty<string>();
        };

        return true;
    }
}
