using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    /// <summary >
    /// A class that implements <see cref="IEqualityComparer{T}"/> where T is <see cref="string"/>, but is set
    /// to explicitly ignore case.
    /// </summary>
    public class IgnoreCase : IEqualityComparer<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IgnoreCase"/> class.
        /// </summary>
        public IgnoreCase() { }

        /// <summary>
        /// Compares and judges two instance of <see cref="string"/> to be equal to one another
        /// while ignoring case.
        /// </summary>
        public bool Equals(string x, string y) => x.Equals(y, StringComparison.CurrentCultureIgnoreCase);
        /// <summary>
        /// Retrieves the <see cref="string"/>'s hash code.
        /// </summary>
        public int GetHashCode(string x) => x.GetHashCode();
    }
}
