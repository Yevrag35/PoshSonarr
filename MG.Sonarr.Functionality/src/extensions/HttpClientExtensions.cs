using System;
using System.Net.Http;

namespace MG.Sonarr.Functionality.Extensions
{
    /// <summary>
    /// Extensions for JSON-specific calls through <see cref="HttpClient"/>.
    /// </summary>
    public static partial class HttpClientExtensions
    {
        private const string CONTENT_TYPE = "application/json";
    }
}
