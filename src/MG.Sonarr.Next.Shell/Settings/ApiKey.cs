using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Shell.Exceptions;
using System.Runtime.InteropServices;
using System.Security;

namespace MG.Sonarr.Next.Shell.Settings
{
    /// <summary>
    /// A wrapping object that allows implicit conversions from <see cref="string"/> and 
    /// <see cref="SecureString"/> objects.
    /// </summary>
    public readonly struct ApiKey : IApiKey
    {
        readonly string? _key;
        readonly bool _isNotEmpty;

        /// <summary>
        /// Indicates whether this <see cref="ApiKey"/> object is <see langword="null"/> or empty.
        /// </summary>
        public bool IsEmpty => !_isNotEmpty;

        /// <summary>
        /// Gets the number of characters in the current <see cref="ApiKey"/> object.
        /// </summary>
        /// <returns>
        ///     The number of characters in the current key.
        /// </returns>
        public int Length => _key?.Length ?? 0;

        private ApiKey(string? key)
        {
            _isNotEmpty = !string.IsNullOrWhiteSpace(key);
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

        /// <summary>
        /// A <see langword="static"/>, empty instance of <see cref="ApiKey"/>.
        /// </summary>
        public static readonly ApiKey Empty = new(string.Empty);
        
        public string GetValue()
        {
            return _key ?? string.Empty;
        }

        /// <summary>
        /// Validates the current key and throws an <see cref="InvalidApiKeyException"/> if validation fails.
        /// </summary>
        /// <exception cref="InvalidApiKeyException"/>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(_key))
            {
                throw new InvalidApiKeyException();
            }
        }

        public static implicit operator ApiKey(string? key) => new(key);
        public static implicit operator ApiKey(SecureString secureString) => new(secureString);
    }
}
