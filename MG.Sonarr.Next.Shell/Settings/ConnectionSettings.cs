using MG.Sonarr.Next.Services.Auth;
using System.Net;

namespace MG.Sonarr.Next.Shell.Settings
{
    internal sealed class ConnectionSettings : IConnectionSettings
    {
        internal ApiKey Key { get; set; }
        IApiKey IConnectionSettings.ApiKey => this.Key;
        public bool NoApiInPath { get; internal set; }
        public Uri ServiceUri { get; internal set; } = null!;
        public bool SkipCertValidation { get; internal set; }
        internal string? ProxyUri { get; set; }
        internal ProxyCredential? ProxyCredential { get; set; }
        public TimeSpan Timeout { get; internal set; } = TimeSpan.FromSeconds(60d);

        public bool TryGetProxy([NotNullWhen(true)] out IWebProxy? proxy)
        {
            proxy = null;
            if (this.ProxyUri is not null)
            {
                proxy = new WebProxy(this.ProxyUri, true, null, this.ProxyCredential);
                return true;
            }

            return false;
        }

        /// <exception cref="ArgumentException"/>
        internal void Validate()
        {
            this.Key.Validate();
            if (this.ServiceUri is null)
            {
                throw new ArgumentException("A base URL must be provided.");
            }
        }
    }
}
