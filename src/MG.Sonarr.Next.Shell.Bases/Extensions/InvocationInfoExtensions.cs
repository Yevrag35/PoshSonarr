using System.Reflection;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Shell.Extensions;

public static class InvocationInfoExtensions
{
    private static bool _hasChecked;
    [MemberNotNullWhen(true, nameof(_positionallyBoundProperty))]
    private static bool CanCheck { get; set; }

    private static PropertyInfo? _positionallyBoundProperty;

    static InvocationInfoExtensions()
    {
        CanCheck = false;
        _hasChecked = false;
    }

    [MemberNotNullWhen(true, nameof(_positionallyBoundProperty))]
    public static bool CheckCanCheckPositionalBinding(Dictionary<string, object?> example)
    {
        if (_hasChecked)
        {
            return CanCheck;
        }

        bool canCheck = TryGetListProperty(example, out _positionallyBoundProperty);
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

        return IsPositionallyBound(parameterName, _positionallyBoundProperty, invocation.BoundParameters);
    }

    private static bool IsPositionallyBound(string parameterName, PropertyInfo listProperty, Dictionary<string, object?> boundParameters)
    {
        ICollection<string>? positionallyBound = (ICollection<string>?)listProperty.GetValue(boundParameters);

        return positionallyBound is not null
            && positionallyBound.Count > 0
            && positionallyBound.Contains(parameterName, StringComparer.OrdinalIgnoreCase);
    }
    private static bool TryGetListProperty(Dictionary<string, object?> boundParameters, [NotNullWhen(true)] out PropertyInfo? listProperty)
    {
        Type type = boundParameters.GetType();
        string propertyName = "BoundPositionally";

        listProperty = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        return listProperty is not null;
    }
}
