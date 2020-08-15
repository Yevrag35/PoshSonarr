using System;
using System.Collections.Generic;
using MG.Sonarr.Results;

namespace MG.Sonarr.Functionality.Url
{
    internal struct UrlParameter : IUrlParameter
    {
        private string _val;

        IConvertible IUrlParameter.Key => this.Key;
        public string Key;
        public int Length => 1 + this.Key.Length + _val.Length;
        public IConvertible Value
        {
            get => _val;
            set => _val = Convert.ToString(value);
        }

        internal UrlParameter(string key, IConvertible value)
        {
            this.Key = key;
            _val = Convert.ToString(value);
        }

        public string AsString() => string.Format("{0}={1}", this.Key, this.Value);
    }
}
