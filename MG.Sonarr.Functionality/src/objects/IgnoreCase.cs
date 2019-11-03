using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    internal class IgnoreCase : IEqualityComparer<string>
    {
        public bool Equals(string x, string y) => x.Equals(y, StringComparison.CurrentCultureIgnoreCase);
        public int GetHashCode(string x) => x.GetHashCode();
    }
}
