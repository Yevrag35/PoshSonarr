using MG.Sonarr.Cmdlets;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace MG.Sonarr
{
    /// <summary>
    /// The static context used by the PoshSonarr module.  The 'Context' is set with the <see cref="ConnectInstance"/> cmdlet.
    /// </summary>
    public static partial class Context
    {
        #region FIELDS/CONSTANTS
        internal const string SLASH_STR = "/";
        internal static readonly char SLASH = char.Parse(SLASH_STR);
        internal const string ZERO_ONE = "{0}{1}";

        [Obsolete]
        private static string _uribase;

        #endregion

        #region PROPERTIES
        /// <summary>
        /// The main <see cref="HttpClient"/> from which all PoshSonarr cmdlets issue their REST requests.
        /// </summary>
        public static SonarrRestClient ApiCaller { get; internal set; }

        /// <summary>
        /// Returns true when <see cref="Context.ApiCaller"/> is not null, has a base address, and contains a valid API key header.
        /// </summary>
        public static bool IsConnected => ApiCaller != null && ApiCaller.BaseAddress != null && ApiCaller.DefaultRequestHeaders.Contains("X-Api-Key");

        [Obsolete]
        public static bool NoApiPrefix = false;

        /// <summary>
        /// The <see cref="SonarrUrl"/> representation of the base URL for all subsequent REST calls.
        /// </summary>
        public static ISonarrUrl SonarrUrl { get; set; }

        /// <summary>
        /// Specifies the additional base uri that <see cref="Context.ApiCaller"/> appends on its API operations.  
        /// A single forward slash ("/") will be interpreted as <see cref="string.Empty"/>.
        /// </summary>
        [Obsolete]
        public static string UriBase
        {
            get => _uribase;
            set => _uribase = value == "/" ? string.Empty : value;
        }

        #endregion

        #region PUBLIC METHODS
        [Obsolete]
        public static void ClearUriBase() => UriBase = null;

        #endregion
    }
}