using System;
using System.Collections.Generic;
using MG.Sonarr.Results;

namespace MG.Sonarr.Functionality.Url
{
    internal struct UrlParameter : IUrlParameter
    {
        internal const string KEY_VALUE_FORMAT = "{0}={1}";
        private string _val;

        private string _key;
        public int Length => 1 + _key.Length + _val.Length;

        private UrlParameter(string key, IConvertible value)
        {
            _key = key;
            _val = Convert.ToString(value);
        }

        internal static IUrlParameter Create(string key, IConvertible value) => new UrlParameter(key, value);

        public string AsString() => string.Format(KEY_VALUE_FORMAT, _key, _val);
        public bool Equals(IUrlParameter other)
        {
            if (other is UrlParameter up)
                return _key.Equals(up._key) && _val.Equals(up._val);

            else
                return false;
        }
    }
}
