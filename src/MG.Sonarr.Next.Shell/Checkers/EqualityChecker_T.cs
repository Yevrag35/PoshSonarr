namespace MG.Sonarr.Next.Shell.Checkers;

public class EqualityChecker<T> : EqualityChecker where T : IEquatable<T>
{
    protected virtual bool Equals([DisallowNull] T x, [DisallowNull] T y)
    {
        return x.Equals(y);
    }

    public int GetHashCode([DisallowNull] T obj)
    {
        return this.GetHashCodeCore(obj);
    }

    protected override bool EqualsCore([DisallowNull] object x, [DisallowNull] object y)
    {
        if (LanguagePrimitives.TryConvertTo(x, out T tX) && LanguagePrimitives.TryConvertTo(y, out T tY))
        {
            return this.Equals(tX!, tY!);
        }

        return false;
    }

    protected override int GetHashCodeCore([DisallowNull] object obj)
    {
        return obj.GetHashCode();
    }
}
