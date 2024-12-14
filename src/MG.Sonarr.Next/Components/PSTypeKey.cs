using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Components;

[DebuggerStepThrough]
[StructLayout(LayoutKind.Auto)]
public readonly struct PSTypeKey : IEquatable<PSTypeKey>
{
    private readonly bool _isNotEmpty;

    public readonly bool IncludeBrackets;
    [MemberNotNullWhen(false, nameof(KeyedType))]
    public readonly bool IsDefaultOrEmpty => !_isNotEmpty;
    public readonly Type? KeyedType;

    private PSTypeKey(Type type)
        : this(type, includeBrackets: false)
    {
    }
    public PSTypeKey(Type type, bool includeBrackets)
    {
        ArgumentNullException.ThrowIfNull(type);
        IncludeBrackets = includeBrackets;
        KeyedType = type;
        _isNotEmpty = true;
    }

    public readonly bool Equals(PSTypeKey other)
    {
        if (this.IsDefaultOrEmpty)
        {
            return other.IsDefaultOrEmpty;
        }

        return IncludeBrackets == other.IncludeBrackets && KeyedType.Equals(other.KeyedType);
    }
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is PSTypeKey other && this.Equals(other);
    }
    public override readonly int GetHashCode()
    {
        if (this.IsDefaultOrEmpty)
        {
            return HashCode.Combine(true, false, nameof(PSTypeKey));
        }

        return HashCode.Combine(IncludeBrackets, KeyedType);
    }

    public static bool operator ==(PSTypeKey left, PSTypeKey right) => left.Equals(right);
    public static bool operator !=(PSTypeKey left, PSTypeKey right) => !(left == right);

    public static implicit operator PSTypeKey(Type type) => new PSTypeKey(type);
}