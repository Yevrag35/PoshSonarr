using System;
using System.Collections;

namespace MG.Sonarr
{
    /// <summary>
    /// An interface providing "Class to JSON-string" methods.
    /// </summary>
    public interface ISonarrResult
    {
        string ToJson();
        string ToJson(IDictionary parameters);
    }
}
