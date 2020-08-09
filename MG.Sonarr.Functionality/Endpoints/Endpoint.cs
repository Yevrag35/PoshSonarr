using MG.Sonarr.Functionality.Strings;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public struct Endpoint
    {
        internal string Value;

        private Endpoint(string apiEndpoint)
        {
            this.Value = apiEndpoint;
        }

        public static implicit operator string(Endpoint endpoint) => endpoint.Value;

        public static Endpoint Backup => new Endpoint(ApiEndpoints.Backup);
        
    }

    public static class EndpointExtensions
    {
        private const string SLASH_API = "/api";

        public static Endpoint WithPrefix(this Endpoint endpoint) => WithPrefix(endpoint, true);
        public static Endpoint WithPrefix(this Endpoint endpoint, bool addIt)
        {
            if (addIt)
                endpoint.Value = SLASH_API + endpoint.Value;

            return endpoint;
        }
    }
}
