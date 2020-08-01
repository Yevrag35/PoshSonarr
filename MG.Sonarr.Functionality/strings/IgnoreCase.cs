using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality.Strings
{
    /// <summary >
    /// A class that implements <see cref="IEqualityComparer{T}"/> where T is <see cref="string"/>, but is set
    /// to explicitly ignore case.
    /// </summary>
    internal class IgnoreCase : IEqualityComparer<string>, IEqualityComparer<object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IgnoreCase"/> class.
        /// </summary>
        internal IgnoreCase() { }

        /// <summary>
        /// Compares and judges two instance of <see cref="string"/> to be equal to one another
        /// while ignoring case.
        /// </summary>
        public bool Equals(string x, string y) => x.Equals(y, StringComparison.InvariantCultureIgnoreCase);
        bool IEqualityComparer<object>.Equals(object x, object y)
        {
            bool result = false;
            if (x is string xStr && y is string yStr)
                result = this.Equals(xStr, yStr);

            return result;
        }

        /// <summary>
        /// Retrieves the <see cref="string"/>'s hash code.
        /// </summary>
        public int GetHashCode(string x) => x.ToLower().GetHashCode();
        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            if (obj is string str)
                return this.GetHashCode(str);

            else
                return obj.GetHashCode();
        }
    }
}
