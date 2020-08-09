using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Functionality.Strings;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public struct Endpoint
    {
        private const string SLASH_ID = "{0}/{1}";
        private static readonly string SPACE = ((char)32).ToString();

        internal string ApiEndpoint;
        internal string Id;
        internal string Prefix;
        internal string Query;

        private Endpoint(string apiEndpoint)
        {
            this.ApiEndpoint = apiEndpoint;
            this.Id = null;
            this.Prefix = null;
            this.Query = null;
        }

        private int GetTotalLength()
        {

            return
                this.ApiEndpoint.Length
                +
                (this.Id?.Length).GetValueOrDefault()
                +
                (this.Prefix?.Length).GetValueOrDefault()
                +
                (this.Query?.Length).GetValueOrDefault();
        }

        public static implicit operator string(Endpoint endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint.ApiEndpoint))
                throw new InvalidOperationException("To form a string, the \"ApiEndpoint\" field must be populated with non-whitespace text.");

            StringBuilder builder = new StringBuilder(endpoint.GetTotalLength());

            builder.Append(endpoint.Prefix);
            builder.Append(endpoint.ApiEndpoint);
            builder.Append(endpoint.Id);
            builder.Append(endpoint.Query);

            builder.Replace(SPACE, string.Empty);
            return builder.ToString();
        }
        public static implicit operator Endpoint(string str) => new Endpoint(str);

        public static Endpoint Backup => new Endpoint(ApiEndpoints.Backup);
        
    }

    public static class EndpointExtensions
    {
        private const string SLASH_API = "/api";
        
        public static Endpoint WithId(this Endpoint endpoint, IConvertible id)
        {
            if (id == null)
                return endpoint;

            endpoint.Id = Convert.ToString(id);
            return endpoint;
        }
        public static Endpoint WithPrefix(this Endpoint endpoint) => WithPrefix(endpoint, true);
        public static Endpoint WithPrefix(this Endpoint endpoint, bool addIt)
        {
            if (addIt)
                endpoint.Prefix = SLASH_API;

            return endpoint;
        }
        public static Endpoint WithQuery(this Endpoint endpoint, IUrlParameterCollection queryParameters)
        {
            endpoint.Query = queryParameters.ToQueryString();
            return endpoint;
        }
    }
}
