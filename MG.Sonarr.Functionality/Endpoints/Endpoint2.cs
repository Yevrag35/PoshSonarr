using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Functionality.Strings;
using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public class Endpoint2
    {
        private StringBuilder _builder;
        private char[] _id;
        private IUrlParameterCollection _query;

        private Endpoint2() => _builder = new StringBuilder(10);
        private Endpoint2(Endpoint2 existing, IUrlParameterCollection urlParameters)
        {
            _builder = new StringBuilder(existing._builder.Length + urlParameters.Count * 8);
            
        }

        private void AddPrefix()
        {
            _builder.Insert(0, ApiEndpoints.API_PREFIX);
        }
        public string Get() => _builder.ToString();

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
        public Endpoint2 WithQuery(IUrlParameterCollection queryParameters, params IUrlParameter[] oneOffs)
        {

        }
    }
}
