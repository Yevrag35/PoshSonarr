using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// An interface exposing functionality around URL parameters in a given query construct.
    /// </summary>
    public interface IUrlParameter
    {
        /// <summary>
        /// The key in the URL query parameter (the value on the left side of '=').
        /// </summary>
        IConvertible Key { get; }
        /// <summary>
        /// The value in the URL query parameter (the value on the right side of '=').
        /// </summary>
        IConvertible Value { get; }

        /// <summary>
        /// Returns the query parameter in its <see cref="Uri"/> query form.
        /// </summary>
        string AsString();
    }
}
