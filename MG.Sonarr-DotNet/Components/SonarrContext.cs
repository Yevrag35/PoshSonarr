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

        #endregion

        #region PROPERTIES
        public static ApiCaller ApiCaller { get; set; }
        public static bool IsConnected => ApiCaller != null && ApiCaller.BaseAddress != null && ApiCaller.DefaultRequestHeaders.Contains("X-Api-Key");
        public static bool NoApiPrefix = false;
        public static string UriBase { get; private set; }

        #endregion

        #region PUBLIC METHODS
        public static void ClearUriBase() => UriBase = null;

        public static void SetUriBase(string uriBase) => UriBase = uriBase;

        //public static void SetReverseProxyUriBase(string uriBase)
        //{
        //    if (!uriBase.StartsWith(SLASH_STR))
        //        uriBase = SLASH + uriBase;

        //    if (uriBase.EndsWith(SLASH_STR))
        //        uriBase = uriBase.TrimEnd(SLASH);

        //    if (!NoApiPrefix)
        //        uriBase = uriBase + "/api";

        //    ReverseProxyUriBase = uriBase;
        //}

        #endregion

        #region BACKEND/PRIVATE METHODS


        #endregion
    }
}