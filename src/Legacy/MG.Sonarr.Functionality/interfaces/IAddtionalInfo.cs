using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Linq;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// An interface providing a method to expose additional information not immediately visible from the JSON object.
    /// </summary>
    public interface IAdditionalInfo
    {
        /// <summary>
        /// Retrieves additional/extra JSON information from the JSON result.
        /// </summary>
        IDictionary GetAdditionalInfo();
    }
}