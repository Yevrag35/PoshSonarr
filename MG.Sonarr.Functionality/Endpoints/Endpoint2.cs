using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Functionality.Strings;
using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public class Endpoint
    {
        private const string SLASH = "/";
        private string _builtString;
        private StringBuilder _builder;
        private char[] _id;
        private bool _isBuilt;
        private IUrlParameterCollection _query;

        public bool IsBuilt => _isBuilt;

        private Endpoint(string endpoint)
        {
            _builder = new StringBuilder(endpoint);
        }
        private Endpoint(Endpoint existing, IUrlParameterCollection urlParameters)
        {
            _builder = new StringBuilder(existing._builder.ToString());
            _id = existing._id;

            _query = urlParameters;
        }

        public static implicit operator string(Endpoint endpoint) => endpoint.Build();

        private void AddPrefix()
        {
            _builder.EnsureCapacity(ApiEndpoints.API_PREFIX.Length);
            _builder.Insert(0, ApiEndpoints.API_PREFIX);
        }
        public string Build()
        {
            if (_isBuilt)
                return _builtString;

            if (_id != null && _id.Length > 0)
            {
                _builder.Append(SLASH);
                _builder.Append(_id);
                Array.Clear(_id, 0, _id.Length);
            }

            if (_query != null && _query.Count > 0)
            {
                _builder.Append(_query.ToQueryString());
                _query.Clear();
            }

            _builtString = _builder.ToString();
            _isBuilt = true;
            _builder.Clear();

            return _builtString;
        }

        public Endpoint WithId(IConvertible icon)
        {
            string str = Convert.ToString(icon);
            _id = new char[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                _id[i] = str[i];
            }
            return this;
        }
        public Endpoint WithPrefix(bool addPrefix)
        {
            if (addPrefix)
                this.AddPrefix();

            return this;
        }
        public Endpoint WithQuery(params IUrlParameter[] oneOffs)
        {
            if (oneOffs == null || oneOffs.Length <= 0)
                return this;

            var col = new UrlParameterCollection(oneOffs);
            return new Endpoint(this, col);
        }
        public Endpoint WithQuery(IUrlParameterCollection queryParameters, params IUrlParameter[] oneOffs)
        {
            var newCol = new UrlParameterCollection(queryParameters.Count + (oneOffs?.Length).GetValueOrDefault());
            newCol.AddRange(queryParameters);
            newCol.AddRange(oneOffs);

            _builder.EnsureCapacity(newCol.Length);
            return new Endpoint(this, newCol);
        }

        #region STATIC CONSTRUCTORS
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
}
