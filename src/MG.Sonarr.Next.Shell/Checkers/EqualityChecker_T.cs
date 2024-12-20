using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Shell.Checkers;

public abstract class EqualityChecker<T> : EqualityChecker
{
    protected abstract bool Equals([DisallowNull] T x, [DisallowNull] T y);

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
