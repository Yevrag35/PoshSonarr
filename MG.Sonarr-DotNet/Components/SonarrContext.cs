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

        #endregion

        #region PROPERTIES

#if DEBUG
        public static List<Quality> AllQualities { get; set; }
#else
        internal static List<Quality> AllQualities { get; set; }
#endif

        /// <summary>
        /// The main <see cref="HttpClient"/> from which all PoshSonarr cmdlets issue their REST requests.
        /// </summary>
        public static SonarrRestClient ApiCaller { get; internal set; }

        /// <summary>
        /// Returns true when <see cref="Context.ApiCaller"/> is not null, has a base address, and contains a valid API key header.
        /// </summary>
        public static bool IsConnected => ApiCaller != null && ApiCaller.BaseAddress != null && ApiCaller.DefaultRequestHeaders.Contains("X-Api-Key");

        /// <summary>
        /// The <see cref="SonarrUrl"/> representation of the base URL for all subsequent REST calls.
        /// </summary>
        internal static ISonarrUrl SonarrUrl { get; set; }

#if DEBUG
        public static TagManager TagManager { get; internal set; }
#else
        internal static TagManager TagManager { get; set; }
#endif

        #endregion
    }
}