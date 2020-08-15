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
            if (_isBuilt)
                return;

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
            if (!_isBuilt)
            {
                string str = Convert.ToString(icon);
                _id = new char[str.Length];
                for (int i = 0; i < str.Length; i++)
                {
                    _id[i] = str[i];
                }
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
        /// <summary>
        /// Points to the endpoint "/backup".
        /// </summary>
        public static Endpoint Backup => new Endpoint(ApiEndpoints.Backup);
        /// <summary>
        /// Points to the endpoint "/calendar".
        /// </summary>
        public static Endpoint Calendar => new Endpoint(ApiEndpoints.Calendar);
        /// <summary>
        /// Points to the endpoint "/command".
        /// </summary>
        public static Endpoint Command => new Endpoint(ApiEndpoints.Command);
        /// <summary>
        /// Points to the endpoint "/delayprofile".
        /// </summary>
        public static Endpoint DelayProfile => new Endpoint(ApiEndpoints.DelayProfile);
        /// <summary>
        /// Points to the endpoint "/diskspace".
        /// </summary>
        public static Endpoint Diskspace => new Endpoint(ApiEndpoints.Diskspace);
        /// <summary>
        /// Points to the endpoint "/downloadclient".
        /// </summary>
        public static Endpoint DownloadClient => new Endpoint(ApiEndpoints.DownloadClient);
        /// <summary>
        /// Points to the endpoint "/episode".
        /// </summary>
        public static Endpoint Episode => new Endpoint(ApiEndpoints.Episode);
        /// <summary>
        /// Points to the endpoint "/episodefile".
        /// </summary>
        public static Endpoint EpisodeFile => new Endpoint(ApiEndpoints.EpisodeFile);
        /// <summary>
        /// Points to the endpoint "/filesystem".
        /// </summary>
        public static Endpoint FileSystem => new Endpoint(ApiEndpoints.FileSystem);
        /// <summary>
        /// Points to the endpoint "/history".
        /// </summary>
        public static Endpoint History => new Endpoint(ApiEndpoints.History);
        /// <summary>
        /// Points to the endpoint "/config/host".
        /// </summary>
        public static Endpoint HostConfig => new Endpoint(ApiEndpoints.HostConfig);
        /// <summary>
        /// Points to the endpoint "/indexer".
        /// </summary>
        public static Endpoint Indexer => new Endpoint(ApiEndpoints.Indexer);
        /// <summary>
        /// Points to the endpoint "/config/indexer".
        /// </summary>
        public static Endpoint IndexerOptions => new Endpoint(ApiEndpoints.IndexerOptions);
        /// <summary>
        /// Points to the endpoint "/indexer/schema".
        /// </summary>
        public static Endpoint IndexerSchema => new Endpoint(ApiEndpoints.IndexerSchema);
        /// <summary>
        /// Points to the endpoint "/log".
        /// </summary>
        public static Endpoint Log => new Endpoint(ApiEndpoints.Log);
        /// <summary>
        /// Points to the endpoint "/log/file".
        /// </summary>
        public static Endpoint LogFile => new Endpoint(ApiEndpoints.LogFile);
        /// <summary>
        /// Points to the endpoint "/manualimport".
        /// </summary>
        public static Endpoint ManualImport => new Endpoint(ApiEndpoints.ManualImport);
        /// <summary>
        /// Points to the endpoint "/mediamanagement".
        /// </summary>
        public static Endpoint MediaManagement => new Endpoint(ApiEndpoints.MediaManagement);
        /// <summary>
        /// Points to the endpoint "/metadata".
        /// </summary>
        public static Endpoint Metadata => new Endpoint(ApiEndpoints.Metadata);
        /// <summary>
        /// Points to the endpoint "/notification".
        /// </summary>
        public static Endpoint Notification => new Endpoint(ApiEndpoints.Notification);
        /// <summary>
        /// Points to the endpoint "/profile".
        /// </summary>
        public static Endpoint Profile => new Endpoint(ApiEndpoints.Profile);
        /// <summary>
        /// Points to the endpoint "/qualitydefinition".
        /// </summary>
        public static Endpoint QualityDefinition => new Endpoint(ApiEndpoints.QualityDefinitions);
        /// <summary>
        /// Points to the endpoint "/queue".
        /// </summary>
        public static Endpoint Queue => new Endpoint(ApiEndpoints.Queue);
        /// <summary>
        /// Points to the endpoint "/release".
        /// </summary>
        public static Endpoint Release => new Endpoint(ApiEndpoints.Release);
        /// <summary>
        /// Points to the endpoint "/remotepathmapping".
        /// </summary>
        public static Endpoint RemotePathMapping => new Endpoint(ApiEndpoints.Mapping);
        /// <summary>
        /// Points to the endpoint "/system/restart".
        /// </summary>
        public static Endpoint Restart => new Endpoint(ApiEndpoints.Restart);
        /// <summary>
        /// Points to the endpoint "/restriction".
        /// </summary>
        public static Endpoint Restriction => new Endpoint(ApiEndpoints.Restriction);
        /// <summary>
        /// Points to the endpoint "/rootfolder".
        /// </summary>
        public static Endpoint RootFolder => new Endpoint(ApiEndpoints.RootFolder);
        /// <summary>
        /// Points to the endpoint "/series".
        /// </summary>
        public static Endpoint Series => new Endpoint(ApiEndpoints.Series);
        /// <summary>
        /// Points to the endpoint "/system/status".
        /// </summary>
        public static Endpoint Status => new Endpoint(ApiEndpoints.Status);
        /// <summary>
        /// Points to the endpoint "/tag".
        /// </summary>
        public static Endpoint Tag => new Endpoint(ApiEndpoints.Tag);
        /// <summary>
        /// Points to the endpoint "/update".
        /// </summary>
        public static Endpoint Update => new Endpoint(ApiEndpoints.Update);
        /// <summary>
        /// Points to the endpoint "/wanted/missing".
        /// </summary>
        public static Endpoint WantedMissing => new Endpoint(ApiEndpoints.WantedMissing);


        #endregion
    }
}
