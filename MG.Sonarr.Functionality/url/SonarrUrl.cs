using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MG.Sonarr.Functionality.Url
{
    internal sealed class SonarrUrl : ISonarrUrl
    {
        #region FIELDS/CONSTANTS
        private UriBuilder _builder;
        private string _base;
        private bool _includeApiPrefix;

        private const string SLASH_STR = "/";
        private const char SLASH = (char)47;
        //private const string SLASH_API = SLASH_STR + "api";
        private const string SLASH_API = "api";

        #endregion

        #region PROPERTIES
        public string BaseUrl => _base;
        public bool IncludeApiPrefix
        {
            get => _includeApiPrefix;
            set
            {
                if (value && !_builder.Path.EndsWith(SLASH_API, StringComparison.CurrentCultureIgnoreCase))
                    this.AddApiPreifx();

                else if (!value && _builder.Path.EndsWith(SLASH_API, StringComparison.CurrentCulture))
                    this.RemoveApiPrefix();

                _includeApiPrefix = value;
            }
        }
        public string Path
        {
            get => _builder.Path != SLASH_STR
                ? _builder.Path.TrimEnd(SLASH)
                : string.Empty;
            set
            {
                _includeApiPrefix = value.EndsWith(SLASH_API);
                _builder.Path = value;
            }
        }
        public Uri Url => _builder.Uri;

        #endregion

        #region CONSTRUCTORS
        public SonarrUrl(Uri url, bool includeApiPrefix)
        {
            this.FormatNew(url.OriginalString, includeApiPrefix);

        }

        public SonarrUrl(string hostName, int portNumber, bool useSsl, string reverseProxyUriBase, bool includeApiPrefix)
        {
            this.FormatNew(hostName, portNumber, useSsl, reverseProxyUriBase, includeApiPrefix);
        }

        #endregion

        #region METHODS
        private void AddApiPreifx()
        {
            _builder.Path = _builder.Path.EndsWith(SLASH_STR)
                ? _builder.Path = _builder.Path + SLASH_API
                : _builder.Path = _builder.Path + SLASH_API;
        }
        private void FormatNew(string url, bool includeApiPrefix)
        {
            _builder = new UriBuilder(url);
            _base = _builder.Uri.GetLeftPart(UriPartial.Scheme | UriPartial.Authority).TrimEnd(SLASH);
            this.IncludeApiPrefix = includeApiPrefix;
        }
        private void FormatNew(string hostName, int portNumber, bool useSsl, string reverseProxyUriBase, bool includeApiPrefix)
        {
            string scheme = !useSsl
                    ? "http"
                    : "https";

            string path = includeApiPrefix
                ? SLASH_API
                : null;

            _builder = new UriBuilder(scheme, hostName, portNumber, path);
            _base = _builder.Uri.GetLeftPart(UriPartial.Scheme | UriPartial.Authority).TrimEnd(SLASH);

            // Add reverse proxy base uri to existing path if specified.
            if (!string.IsNullOrEmpty(reverseProxyUriBase))
            {
                reverseProxyUriBase = reverseProxyUriBase.Trim(SLASH);
                if (reverseProxyUriBase.IndexOf(SLASH_API, StringComparison.CurrentCultureIgnoreCase) >= 0 &&
                    _builder.Path.Contains(SLASH_API))
                {
                    reverseProxyUriBase = reverseProxyUriBase.Replace(SLASH_API, string.Empty);
                }
                _builder.Path = SLASH_STR + reverseProxyUriBase + _builder.Path;
            }
        }
        private void RemoveApiPrefix() => _builder.Path = _builder.Path.Substring(0, _builder.Path.LastIndexOf(SLASH_API));

        #endregion
    }
}