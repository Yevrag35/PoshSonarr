using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MG.Sonarr
{
    /// <summary>
    /// An exception thrown when parsing a JSON response body that contains malformed data or is blank or null.
    /// </summary>
    public class ParseJsonResponseException : HttpRequestException
    {
        private const string DEF_MSG = "Failed to parse JSON response into the specified object.  The response content was malformed, blank, or null.";

        /// <summary>
        /// The default constructor.
        /// </summary>
        public ParseJsonResponseException() : base(DEF_MSG) { }

        /// <summary>
        /// Initializes a new instance of <see cref="ParseJsonResponseException"/> with the specified <see cref="string"/> as the source.
        /// </summary>
        /// <param name="source">The source of the exception.</param>
        public ParseJsonResponseException(string source) : base(DEF_MSG) => this.Source = source;
    }
}
