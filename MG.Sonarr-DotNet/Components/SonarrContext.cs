using MG.Sonarr.Functionality;
using MG.Sonarr.Cmdlets;
using MG.Sonarr.Results;
using System;
using System.Net.Http;
using System.Management.Automation;

namespace MG.Sonarr
{
    /// <summary>
    /// The static context used by the PoshSonarr module.  The 'Context' is set with the <see cref="ConnectInstance"/> cmdlet.
    /// </summary>
    public static partial class Context
    {
        #region FIELDS/CONSTANTS
        //internal const string SLASH_STR = "/";
        internal const char SLASH = (char)47;
        internal const string ZERO_ONE = "{0}{1}";

        #endregion

        internal static PSObject GetConnectionStatus()
        {
            var pso = new PSObject();
            pso.Properties.Add(new PSNoteProperty("IsConnected", IsConnected));
            pso.Properties.Add(new PSNoteProperty("SonarrUrl", SonarrUrl?.Url));
            return pso;
        }

        #region PROPERTIES

#if DEBUG
        public static QualityDictionary AllQualities { get; set; }
#else
        internal static QualityDictionary AllQualities { get; set; }
#endif

        /// <summary>
        /// The main <see cref="HttpClient"/> from which all PoshSonarr cmdlets issue their REST requests.
        /// </summary>
#if DEBUG
        public static SonarrRestClient ApiCaller { get; internal set; }
#else
        internal static SonarrRestClient ApiCaller { get; set; }
#endif

#if DEBUG
        public static IndexerSchemaDictionary IndexerSchemas { get; internal set; }
#else
        internal static IndexerSchemaDictionary IndexerSchemas { get; set; }
#endif

        /// <summary>
        /// Returns true when <see cref="Context.ApiCaller"/> is not null, has a base address, and contains a valid API key header.
        /// </summary>
#if DEBUG
        public static bool IsConnected => ApiCaller != null && ApiCaller.BaseAddress != null && ApiCaller.DefaultRequestHeaders.Contains("X-Api-Key");
#else
        internal static bool IsConnected => ApiCaller != null && ApiCaller.BaseAddress != null && ApiCaller.DefaultRequestHeaders.Contains("X-Api-Key");
#endif

        /// <summary>
        /// The <see cref="SonarrUrl"/> representation of the base URL for all subsequent REST calls.
        /// </summary>
#if DEBUG
        public static ISonarrUrl SonarrUrl { get; internal set; }
#else
        internal static ISonarrUrl SonarrUrl { get; set; }
#endif

#if DEBUG
        public static TagManager TagManager { get; internal set; }
#else
        internal static TagManager TagManager { get; set; }
#endif

#endregion
    }
}