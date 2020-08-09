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
        private static readonly string SPACE = ((char)32).ToString();
        private StringBuilder _builder;

        internal string ApiEndpoint;
        internal string Id;
        internal string Prefix;
        internal string Query;

        private Endpoint(string apiEndpoint)
        {
            _builder = null;

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
        public string AsString()
        {
            if (string.IsNullOrWhiteSpace(this.ApiEndpoint))
                return this.ToString();

            if (_builder == null)
                _builder = new StringBuilder(this.GetTotalLength());

            else
                _builder.EnsureCapacity(this.GetTotalLength());

            _builder.Append(this.Prefix);
            _builder.Append(this.ApiEndpoint);
            _builder.Append(this.Id);
            _builder.Append(this.Query);

            _builder.Replace(SPACE, string.Empty);
            string built = _builder.ToString();

            _builder.Clear();
            return built;
        }

        public static implicit operator string(Endpoint endpoint) => endpoint.AsString();
        public static implicit operator Endpoint(string str) => new Endpoint(str);

        #region CONSTRUCTS
        public static Endpoint Backup => new Endpoint(ApiEndpoints.Backup);
        public static Endpoint Calendar => new Endpoint(ApiEndpoints.Calendar);
        public static Endpoint Command => new Endpoint(ApiEndpoints.Command);
        public static Endpoint DelayProfile => new Endpoint(ApiEndpoints.DelayProfile);
        public static Endpoint Diskspace => new Endpoint(ApiEndpoints.Diskspace);
        public static Endpoint DownloadClient => new Endpoint(ApiEndpoints.DownloadClient);
        public static Endpoint Episode => new Endpoint(ApiEndpoints.Episode);
        public static Endpoint EpisodeFile => new Endpoint(ApiEndpoints.EpisodeFile);
        public static Endpoint FileSystem => new Endpoint(ApiEndpoints.FileSystem);
        public static Endpoint History => new Endpoint(ApiEndpoints.History);
        public static Endpoint HostConfig => new Endpoint(ApiEndpoints.HostConfig);
        public static Endpoint Indexer => new Endpoint(ApiEndpoints.Indexer);
        public static Endpoint IndexerOptions => new Endpoint(ApiEndpoints.IndexerOptions);
        public static Endpoint IndexerSchema => new Endpoint(ApiEndpoints.IndexerSchema);
        public static Endpoint LogFile => new Endpoint(ApiEndpoints.LogFile);
        public static Endpoint ManualImport => new Endpoint(ApiEndpoints.ManualImport);
        public static Endpoint MediaManagement => new Endpoint(ApiEndpoints.MediaManagement);
        public static Endpoint Metadata => new Endpoint(ApiEndpoints.Metadata);
        public static Endpoint Notification => new Endpoint(ApiEndpoints.Notification);
        public static Endpoint Profile => new Endpoint(ApiEndpoints.Profile);
        public static Endpoint QualityDefinition => new Endpoint(ApiEndpoints.QualityDefinitions);
        public static Endpoint Queue => new Endpoint(ApiEndpoints.Queue);
        public static Endpoint Release => new Endpoint(ApiEndpoints.Release);
        public static Endpoint RemotePathMapping => new Endpoint(ApiEndpoints.Mapping);
        public static Endpoint Restart => new Endpoint(ApiEndpoints.Restart);
        public static Endpoint Restriction => new Endpoint(ApiEndpoints.Restriction);
        public static Endpoint RootFolder => new Endpoint(ApiEndpoints.RootFolder);
        public static Endpoint Series => new Endpoint(ApiEndpoints.Series);
        public static Endpoint Status => new Endpoint(ApiEndpoints.Status);
        public static Endpoint Tag => new Endpoint(ApiEndpoints.Tag);
        public static Endpoint Update => new Endpoint(ApiEndpoints.Update);
        public static Endpoint WantedMissing => new Endpoint(ApiEndpoints.WantedMissing);

        #endregion
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
