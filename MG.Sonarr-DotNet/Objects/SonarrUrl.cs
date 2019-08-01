using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MG.Sonarr
{
    public class SonarrUrl : ISonarrUrl
    {
        #region FIELDS/CONSTANTS
        private UriBuilder _builder;
        private string _base;
        private bool _includeApiPrefix;

        private const string SLASH_STR = "/";
        private static readonly char SLASH = char.Parse(SLASH_STR);
        private static readonly char[] SLASH_API = new char[4]
        {
            SLASH, char.Parse("a"), char.Parse("p"), char.Parse("i")
        };

        #endregion

        #region PROPERTIES
        public string BaseUrl => _base;
        public bool IncludeApiPrefix
        {
            get => _includeApiPrefix;
            set
            {
                if (value && !_builder.Path.EndsWith("/api", StringComparison.CurrentCultureIgnoreCase))
                    this.AddApiPreifx();

                else if (!value && _builder.Path.EndsWith("/api", StringComparison.CurrentCulture))
                    this.RemoveApiPrefix();

                _includeApiPrefix = value;
            }
        }
        public string Path
        {
            get => _builder.Path != "/"
                ? _builder.Path.TrimEnd(SLASH)
                : string.Empty;
            set
            {
                _includeApiPrefix = value.EndsWith("/api");
                _builder.Path = value;
            }
        }
        public Uri Url => _builder.Uri;

        #endregion

        #region CONSTRUCTORS
        public SonarrUrl(Uri url, bool includeApiPrefix) => this.FormatNew(url, includeApiPrefix);

        public SonarrUrl(string hostName, int portNumber, bool useSsl, string reverseProxyUriBase, bool includeApiPrefix)
        {
            this.FormatNew(hostName, portNumber, useSsl, reverseProxyUriBase, includeApiPrefix);
        }

        #endregion

        #region METHODS
        private void AddApiPreifx()
        {
            _builder.Path = _builder.Path.EndsWith("/")
                ? _builder.Path = _builder.Path + "api"
                : _builder.Path = _builder.Path + "/api";
        }

        private void FormatNew(Uri url, bool includeApiPrefix)
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
                ? "/api"
                : null;

            _builder = new UriBuilder(scheme, hostName, portNumber, path);
            _base = _builder.Uri.GetLeftPart(UriPartial.Scheme | UriPartial.Authority).TrimEnd(SLASH);

            // Add reverse proxy base uri to existing path if specified.
            if (!string.IsNullOrEmpty(reverseProxyUriBase))
            {
                reverseProxyUriBase = reverseProxyUriBase.Trim(SLASH);
                if (reverseProxyUriBase.IndexOf("/api", StringComparison.CurrentCultureIgnoreCase) >= 0 &&
                    _builder.Path.Contains("/api"))
                {
                    reverseProxyUriBase = reverseProxyUriBase.Replace("/api", string.Empty);
                }
                _builder.Path = SLASH_STR + reverseProxyUriBase + _builder.Path;
            }
        }

        private void RemoveApiPrefix() => _builder.Path = _builder.Path.Trim(SLASH_API);

        #endregion
    }
}