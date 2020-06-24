using System;
using System.Collections.Generic;
using System.Net;

namespace MG.Sonarr.Functionality
{
    public class FolderParameter : IUrlParameter
    {
        IConvertible IUrlParameter.Key => this.Key;
        public string Key => "folder";

        IConvertible IUrlParameter.Value => this.Value;
        public string Value { get; }

        public FolderParameter(string path) => this.Value = path;

        public string AsString() => string.Format("{0}={1}", this.Key, WebUtility.UrlEncode(this.Value));
    }
}
