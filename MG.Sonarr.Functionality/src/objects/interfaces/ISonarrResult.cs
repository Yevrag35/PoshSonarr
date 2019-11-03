using System;
using System.Collections;

namespace MG.Sonarr
{
    /// <summary>
    /// Provides JSON-serialization methods for the implementing class.
    /// </summary>
    public interface IJsonResult
    {
        /// <summary>
        /// Returns the JSON-formatted <see cref="string"/> representation of the implementing class.
        /// </summary>
        string ToJson();
        /// <summary>
        /// Returns the JSON-formatted <see cref="string"/> representation of the implementing class while
        /// adding the entries of the specified <see cref="IDictionary"/> to the result.
        /// </summary>
        /// <param name="parameters">The non-generic dictionary whose entries will added to JSON string result.</param>
        /// <returns></returns>
        string ToJson(IDictionary parameters);
    }
}
