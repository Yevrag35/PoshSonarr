using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MG.Sonarr
{
    public static class Context
    {
        #region FIELDS/CONSTANTS


        #endregion

        #region PROPERTIES
        public static ApiCaller ApiCaller { get; set; }
        public static bool IsConnected => ApiCaller != null && ApiCaller.BaseAddress != null && ApiCaller.DefaultRequestHeaders.Contains("X-Api-Key");
        public static bool NoApiPrefix = false;

        #endregion

        #region BACKEND/PRIVATE METHODS


        #endregion
    }
}