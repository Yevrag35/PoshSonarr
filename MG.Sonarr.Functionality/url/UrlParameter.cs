using System;
using System.Collections.Generic;
using MG.Sonarr.Results;

namespace MG.Sonarr.Functionality.Url
{
    internal struct UrlParameter : IUrlParameter
    {
        internal const string KEY_VALUE_FORMAT = "{0}={1}";
        private string _val;

        public string Key;
        public int Length => 1 + this.Key.Length + _val.Length;

        private UrlParameter(string key, IConvertible value)
        {
            this.Key = key;
            _val = Convert.ToString(value);
        }

        internal static IUrlParameter Create(string key, IConvertible value) => new UrlParameter(key, value);

        public string AsString() => string.Format(KEY_VALUE_FORMAT, this.Key, _val);
    }
}
