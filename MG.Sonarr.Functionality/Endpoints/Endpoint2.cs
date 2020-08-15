using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Functionality.Strings;
using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public class Endpoint2
    {
        private const string SLASH = "/";
        private string _builtString;
        private StringBuilder _builder;
        private char[] _id;
        private bool _isBuilt;
        private IUrlParameterCollection _query;

        public bool IsBuilt => _isBuilt;

        private Endpoint2(string endpoint)
        {
            _builder = new StringBuilder(endpoint);
        }
        private Endpoint2(Endpoint2 existing, IUrlParameterCollection urlParameters)
        {
            _builder = new StringBuilder(existing._builder.ToString());
            _id = existing._id;

            _query = urlParameters;
        }

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

        public Endpoint2 WithId(IConvertible icon)
        {
            string str = Convert.ToString(icon);
            _id = new char[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                _id[i] = str[i];
            }
            return this;
        }
        public Endpoint2 WithPrefix(bool addPrefix)
        {
            if (addPrefix)
                this.AddPrefix();

            return this;
        }
        public Endpoint2 WithQuery(params IUrlParameter[] oneOffs)
        {
            if (oneOffs == null || oneOffs.Length <= 0)
                return this;

            var col = new UrlParameterCollection(oneOffs);
            return new Endpoint2(this, col);
        }
        public Endpoint2 WithQuery(IUrlParameterCollection queryParameters, params IUrlParameter[] oneOffs)
        {
            var newCol = new UrlParameterCollection(queryParameters.Count + (oneOffs?.Length).GetValueOrDefault());
            newCol.AddRange(queryParameters);
            newCol.AddRange(oneOffs);

            _builder.EnsureCapacity(newCol.Length);
            return new Endpoint2(this, newCol);
        }

        #region STATIC CONSTRUCTORS
        public static Endpoint2 Backup => new Endpoint2(ApiEndpoints.Backup);
        public static Endpoint2 Calendar => new Endpoint2(ApiEndpoints.Calendar);
        public static Endpoint2 Command => new Endpoint2(ApiEndpoints.Command);
        public static Endpoint2 DelayProfile => new Endpoint2(ApiEndpoints.DelayProfile);
        public static Endpoint2 Diskspace => new Endpoint2(ApiEndpoints.Diskspace);
        public static Endpoint2 DownloadClient => new Endpoint2(ApiEndpoints.DownloadClient);
        public static Endpoint2 Episode => new Endpoint2(ApiEndpoints.Episode);
        public static Endpoint2 EpisodeFile => new Endpoint2(ApiEndpoints.EpisodeFile);
        public static Endpoint2 FileSystem => new Endpoint2(ApiEndpoints.FileSystem);
        public static Endpoint2 History => new Endpoint2(ApiEndpoints.History);
        public static Endpoint2 HostConfig => new Endpoint2(ApiEndpoints.HostConfig);
        public static Endpoint2 Indexer => new Endpoint2(ApiEndpoints.Indexer);
        public static Endpoint2 IndexerOptions => new Endpoint2(ApiEndpoints.IndexerOptions);
        public static Endpoint2 IndexerSchema => new Endpoint2(ApiEndpoints.IndexerSchema);
        public static Endpoint2 LogFile => new Endpoint2(ApiEndpoints.LogFile);
        public static Endpoint2 ManualImport => new Endpoint2(ApiEndpoints.ManualImport);
        public static Endpoint2 MediaManagement => new Endpoint2(ApiEndpoints.MediaManagement);
        public static Endpoint2 Metadata => new Endpoint2(ApiEndpoints.Metadata);
        public static Endpoint2 Notification => new Endpoint2(ApiEndpoints.Notification);
        public static Endpoint2 Profile => new Endpoint2(ApiEndpoints.Profile);
        public static Endpoint2 QualityDefinition => new Endpoint2(ApiEndpoints.QualityDefinitions);
        public static Endpoint2 Queue => new Endpoint2(ApiEndpoints.Queue);
        public static Endpoint2 Release => new Endpoint2(ApiEndpoints.Release);
        public static Endpoint2 RemotePathMapping => new Endpoint2(ApiEndpoints.Mapping);
        public static Endpoint2 Restart => new Endpoint2(ApiEndpoints.Restart);
        public static Endpoint2 Restriction => new Endpoint2(ApiEndpoints.Restriction);
        public static Endpoint2 RootFolder => new Endpoint2(ApiEndpoints.RootFolder);
        public static Endpoint2 Series => new Endpoint2(ApiEndpoints.Series);
        public static Endpoint2 Status => new Endpoint2(ApiEndpoints.Status);
        public static Endpoint2 Tag => new Endpoint2(ApiEndpoints.Tag);
        public static Endpoint2 Update => new Endpoint2(ApiEndpoints.Update);
        public static Endpoint2 WantedMissing => new Endpoint2(ApiEndpoints.WantedMissing);


        #endregion
    }
}
