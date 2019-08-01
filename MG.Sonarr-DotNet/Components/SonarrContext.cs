﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace MG.Sonarr
{
    public static class Context
    {
        #region FIELDS/CONSTANTS
        private const string SLASH_STR = "/";
        private static readonly char SLASH = char.Parse(SLASH_STR);
        internal const string ZERO_ONE = "{0}{1}";

        internal static int? PSVersionMajor { get; set; }

        [Obsolete]
        private static string _uribase;

        #endregion

        #region PROPERTIES
        /// <summary>
        /// The main <see cref="HttpClient"/> from which all PoshSonarr cmdlets issue their API requests.
        /// </summary>
        public static ApiCaller ApiCaller { get; internal set; }

        /// <summary>
        /// Returns true when <see cref="Context.ApiCaller"/> is not null, has a base address, and contains a valid API key header.
        /// </summary>
        public static bool IsConnected => ApiCaller != null && ApiCaller.BaseAddress != null && ApiCaller.DefaultRequestHeaders.Contains("X-Api-Key");

        [Obsolete]
        public static bool NoApiPrefix = false;

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