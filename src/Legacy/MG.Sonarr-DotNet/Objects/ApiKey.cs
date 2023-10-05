using MG.Sonarr.Functionality.Exceptions;
using MG.Sonarr.Functionality.Strings;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;

namespace MG.Sonarr
{
    /// <summary>
    /// A validator class to verify a <see cref="string"/> or <see cref="SecureString"/> can become an appropriate API key for use with Sonarr.
    /// </summary>
    public class ApiKey : IApiKey
    {
        #region FIELDS/CONSTANTS
        private const string HEADER_PARAM = "X-Api-Key";
        private const string KEY_PATTERN = @"^(?:[0-9]|[a-z]){32}$";

        #endregion

        #region PROPERTIES
        /// <summary>
        /// The <see cref="string"/>-representation of the API key.
        /// </summary>
        public string Key { get; }

        #endregion

        #region CONSTRUCTORS
        private ApiKey(string keyStr)
        {
            if (!this.IsValidKey(keyStr))
                throw new InvalidApiKeyException();

            else
                this.Key = keyStr.ToLower();
        }
        private ApiKey(SecureString keySs)
            : this(ConvertFromSecureString(keySs)) { }

        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Transforms the string API key into a key value pair for use as a HTTP header.  The 'key' of the key value pair will always be 'X-Api-Key'.
        /// </summary>
        public ValueTuple<string, string> ToTuple() => new ValueTuple<string, string>(HEADER_PARAM, this.Key);

        #endregion

        #region OPERATORS/CASTS
        public static implicit operator ApiKey(string str) => new ApiKey(str);
        public static implicit operator ApiKey(SecureString ss) => new ApiKey(ss);

        #endregion

        #region BACKEND/PRIVATE METHODS
        /// <summary>
        /// Converts a given <see cref="SecureString"/> into its plain-text string form.
        /// </summary>
        /// <param name="ss">The <see cref="SecureString"/> to decrypt.</param>
        /// <returns></returns>
        private static string ConvertFromSecureString(SecureString ss)
        {
            IntPtr pp = Marshal.SecureStringToBSTR(ss);
            string s = Marshal.PtrToStringAuto(pp);
            Marshal.ZeroFreeBSTR(pp);
            return s;
        }

        /// <summary>
        /// Tests a given <see cref="string"/> against a Regex pattern to verify it is 32 characters in length and only consists of lowercase characters.
        /// </summary>
        /// <param name="testStr">The string to validate.</param>
        /// <returns></returns>
        private bool IsValidKey(string testStr) => Regex.IsMatch(testStr, KEY_PATTERN, RegexOptions.IgnoreCase);

        #endregion
    }
}