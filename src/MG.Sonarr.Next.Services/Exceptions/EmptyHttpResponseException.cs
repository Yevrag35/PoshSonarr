using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Services.Exceptions
{
    public sealed class EmptyHttpResponseException : Exception
    {
        public string Url { get; }

        public EmptyHttpResponseException(string url)
            : base("An empty response was received when there should have been one.")
        {
            this.Url = url;
        }
    }
}
