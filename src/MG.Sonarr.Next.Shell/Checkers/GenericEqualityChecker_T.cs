using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Shell.Checkers;

public sealed class GenericEqualityChecker<T> : EqualityChecker<T> where T : IEquatable<T>
{
    protected override bool Equals(T x, T y)
    {
        return x.Equals(y);
    }
}
