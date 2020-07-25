using MG.Api.Json;
using Newtonsoft.Json;
using System;
using System.Collections;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// Provides JSON-serialization methods for the implementing class.
    /// </summary>
    public interface IJsonResult : IJsonObject
    {
        ///// <summary>
        ///// Returns the JSON-formatted <see cref="string"/> representation of the implementing class.
        ///// </summary>
        ///// <exception cref="JsonSerializationException"/>
        //string ToJson();
        ///// <summary>
        ///// Returns the JSON-formatted <see cref="string"/> representation of the implementing class while
        ///// adding the entries of the specified <see cref="IDictionary"/> to the result.
        ///// </summary>
        ///// <param name="parameters">The non-generic dictionary whose entries will added to JSON string result.</param>
        ///// <exception cref="JsonSerializationException"/>
        //string ToJson(IDictionary parameters);
    }
}
