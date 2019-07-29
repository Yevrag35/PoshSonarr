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
    public static class Context
    {
        #region FIELDS/CONSTANTS
        private const string SLASH_STR = "/";
        private static readonly char SLASH = char.Parse(SLASH_STR);

        private static string _uribase;

        #endregion

        #region PROPERTIES
        public static ApiCaller ApiCaller { get; set; }
        public static bool IsConnected => ApiCaller != null && ApiCaller.BaseAddress != null && ApiCaller.DefaultRequestHeaders.Contains("X-Api-Key");
        public static bool NoApiPrefix = false;
        public static string UriBase
        {
            get => _uribase;
            set => _uribase = value == "/" ? string.Empty : value;
        }

        #endregion

        #region PUBLIC METHODS
        public static void ClearUriBase() => UriBase = null;

        #endregion

        #region BACKEND/PRIVATE METHODS


        #endregion
    }
}