using System;
using System.Collections.Generic;
using MG.Sonarr.Results;

namespace MG.Sonarr.Functionality.Url
{
    internal struct UrlParameter : IUrlParameter
    {
        private IConvertible _val;

        IConvertible IUrlParameter.Key => this.Key;
        public string Key;

        public IConvertible Value
        {
            get => _val;
            set => _val = value;
        }

        internal UrlParameter(string key, IConvertible value)
        {
            this.Key = key;
            _val = value;
        }

        public string AsString() => string.Format("{0}={1}", this.Key, this.Value);
    }
}
