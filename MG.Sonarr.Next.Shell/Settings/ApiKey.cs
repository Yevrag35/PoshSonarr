using MG.Sonarr.Next.Services.Auth;
using System.Runtime.InteropServices;
using System.Security;

namespace MG.Sonarr.Next.Shell.Settings
{
    public readonly struct ApiKey : IApiKey
    {
        readonly string? _key;

        private ApiKey(string? key)
        {
            _key = key ?? string.Empty;
        }
        private ApiKey(SecureString secureString)
            : this(ConvertFromSecureString(secureString))
        {
        }

        /// <summary>
        /// Converts a given <see cref="SecureString"/> into its plain-text string form.
        /// </summary>
        /// <param name="ss">The <see cref="SecureString"/> to decrypt.</param>
        /// <returns></returns>
        private static string? ConvertFromSecureString(SecureString ss)
        {
            IntPtr pp = Marshal.SecureStringToBSTR(ss);
            string? s = Marshal.PtrToStringAuto(pp);
            Marshal.ZeroFreeBSTR(pp);
            return s ?? string.Empty;
        }
        
        /// <exception cref="InvalidOperationException"/>
        public string GetValue()
        {
            return _key ?? throw new InvalidOperationException("No API key is present.");
        }

        /// <exception cref="ArgumentException"></exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(_key))
            {
                throw new ArgumentException("The API key cannot be null, empty, or whitespace.");
            }
        }

        public static implicit operator ApiKey(string? key) => new(key);
        public static implicit operator ApiKey(SecureString secureString) => new(secureString);
    }
}
