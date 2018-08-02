using System;
using System.Text.RegularExpressions;

namespace Sonarr.Api.Authentication
{
    public class ApiKey : Object
    {
        private const int _keyLength = 32;
        private const string _regExp = @"^(?:[0-9]|[a-z]){32}$";
        private string _key;
        public string Key { get { return _key; } }

        public ApiKey(string inputKey)
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
        private bool ValidateFormat(string testString)
        {
            return Regex.IsMatch(testString, _regExp);
        }
    }
}
