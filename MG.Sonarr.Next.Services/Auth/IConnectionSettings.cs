﻿using System.Net;

namespace MG.Sonarr.Next.Services.Auth
{
    public interface IConnectionSettings
    {
        IApiKey ApiKey { get; }
        bool NoApiInPath { get; }
        Uri ServiceUri { get; }
        bool SkipCertValidation { get; }
        TimeSpan Timeout { get; }

        bool TryGetProxy([NotNullWhen(true)] out IWebProxy? proxy);
    }
}