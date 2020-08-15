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
        /// The total character length of the <see cref="string"/> result when calling <see cref="IUrlParameter.AsString()"/>.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Returns the query parameter in its <see cref="Uri"/> query form.
        /// </summary>
        string AsString();
    }
}
