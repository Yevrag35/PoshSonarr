using System.Collections;

namespace MG.Sonarr.Next.Shell.Checkers;

[DebuggerStepThrough]
public abstract class EqualityChecker : IEqualityComparer, IEqualityComparer<object>
{
    public new bool Equals(object? x, object? y)
    {
        if (x is null)
        {
            return y is null;
        }
        else if (IsNullOrReferenceEquals(x, y, out bool result))
        {
            return result;
        }

        return this.EqualsCore(x, y);
    }

    protected abstract bool EqualsCore([DisallowNull] object x, [DisallowNull] object y);

    public int GetHashCode([DisallowNull] object obj)
    {
        return this.GetHashCodeCore(obj);
    }
    protected abstract int GetHashCodeCore([DisallowNull] object obj);

    private static bool IsNullOrReferenceEquals([DisallowNull] object x, [NotNullWhen(false)] object? y, out bool result)
    {
        if (y is null)
        {
            result = false;
            return true;
        }
        else if (ReferenceEquals(x, y))
        {
            result = true;
            return true;
        }

        result = false;
        return false;
    }

    public static readonly EqualityChecker Default = new DefaultChecker();

    private sealed class DefaultChecker : EqualityChecker
    {
        protected override bool EqualsCore([DisallowNull] object x, [DisallowNull] object y)
        {
            return LanguagePrimitives.Equals(x, y, ignoreCase: true, Statics.DefaultProvider);
        }

        protected override int GetHashCodeCore([DisallowNull] object obj)
        {
            return obj.GetHashCode();
        }
    }
}