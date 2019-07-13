using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;

namespace MG.Sonarr
{
    public class ApiKey
    {
        #region FIELDS/CONSTANTS
        private const string HEADER_PARAM = "X-Api-Key";
        private const int KEY_LENGTH = 32;
        private const string KEY_PATTERN = @"^(?:[0-9]|[a-z]){32}$";

        #endregion

        #region PROPERTIES
        public string Key { get; }

        #endregion

        #region CONSTRUCTORS
        private ApiKey(string keyStr)
        {
            if (!this.IsValidKey(keyStr))
                throw new InvalidApiKeyException();

            else
                this.Key = keyStr;
        }
        private ApiKey(SecureString keySs)
            : this(ConvertFromSecureString(keySs)) { }

        #endregion

        #region PUBLIC METHODS
        public KeyValuePair<string, string> AsKeyValuePair() => new KeyValuePair<string, string>(HEADER_PARAM, this.Key);

        #endregion

        #region OPERATORS/CASTS
        public static implicit operator ApiKey(string str) => new ApiKey(str);
        public static implicit operator ApiKey(SecureString ss) => new ApiKey(ss);

        #endregion

        #region BACKEND/PRIVATE METHODS
        private static string ConvertFromSecureString(SecureString ss)
        {
            IntPtr pp = Marshal.SecureStringToBSTR(ss);
            string s = Marshal.PtrToStringAuto(pp);
            Marshal.ZeroFreeBSTR(pp);
            return s;
        }
        private bool IsValidKey(string testStr) => Regex.IsMatch(testStr, KEY_PATTERN);

        #endregion
    }
}