using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security;

namespace MG.Sonarr
{
    /// <summary>
    /// Provides the functionality for a implementing class to validate a <see cref="string"/> or <see cref="SecureString"/>
    /// for use as an API key in an <see cref="HttpClient"/>'s header collection.
    /// </summary>
    public interface IApiKey
    {
        /// <summary>
        /// The <see cref="string"/>-representation of the API key.
        /// </summary>
        string Key { get; }
        /// <summary>
        /// Creates the implementing class as a key-value pair so it can be added easily to <see cref="WebHeaderCollection"/>.
        /// </summary>
        /// <returns></returns>
        KeyValuePair<string, string> AsKeyValuePair();
    }
}
