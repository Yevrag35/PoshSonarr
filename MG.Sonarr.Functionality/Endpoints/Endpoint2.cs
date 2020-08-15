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

        public static Endpoint2 Series => new Endpoint2(ApiEndpoints.Series);
    }
}
