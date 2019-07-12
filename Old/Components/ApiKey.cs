using MG.Api;
//using Sonarr.Api.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;

namespace Sonarr.Api
{
    public class ApiKey
    {
        #region Constants and Properties
        private readonly string _key;
        private protected const int _keyLength = 32;
        private protected const string _regExp = @"^(?:[0-9]|[a-z]){32}$";
        public string Value => _key;

        #endregion

        #region Constructors
        private ApiKey(string inputKey)
        {
            if (!ValidateFormat(inputKey))
            {
                throw new Exception("The input key is not in the valid format of 32, all lowercase, characters!");
            }
            else
            {
                _key = inputKey;
            }
        }
        private ApiKey(SecureString secureKey)
        {
            var s = ConvertFromSecureString(secureKey);
            if (!ValidateFormat(s))
            {
                throw new Exception("The input key is not in the valid format of 32, all lowercase, characters!");
            }
            else
            {
                _key = s;
            }
        }

        #endregion

        #region Implicit Casts
        public static implicit operator string(ApiKey apiKey) => apiKey.ToString();
        public static implicit operator ApiKey(string s) => new ApiKey(s);
        public static implicit operator ApiKey(SecureString ss) => new ApiKey(ss);
        public static implicit operator SecureString(ApiKey apiKey)
        {
            var ss = new SecureString();
            for (int i = 0; i < apiKey.Value.Length; i++)
            {
                char c = apiKey.Value[i];
                ss.AppendChar(c);
            }
            return ss;
        }

        #endregion

        #region Methods
        private bool ValidateFormat(string testString) => Regex.IsMatch(testString, _regExp);
        private string ConvertFromSecureString(SecureString ss)
        {
            IntPtr pp = Marshal.SecureStringToBSTR(ss);
            var s = Marshal.PtrToStringAuto(pp);
            Marshal.ZeroFreeBSTR(pp);
            return s;
        }

        public WebHeaderCollection AsSonarrHeader() =>
            new WebHeaderCollection() { { "X-Api-Key", _key } };

        #endregion
    }
}
