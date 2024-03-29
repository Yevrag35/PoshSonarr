﻿using System.Management.Automation;
using System.Net;

namespace MG.Sonarr.Next.Services.Auth
{
    /// <summary>
    /// A wrapper class for <see cref="NetworkCredential"/> that can implicitly convert 
    /// <see cref="PSCredential"/> to <see cref="ICredentials"/>.
    /// </summary>
    public sealed class ProxyCredential : ICredentials
    {
        readonly ICredentials _nc;

        public ProxyCredential(ICredentials icreds) => _nc = icreds;
        public ProxyCredential(PSCredential psCreds)
            : this(psCreds.GetNetworkCredential()) { }

        public static implicit operator ProxyCredential(PSCredential psCreds) => new(psCreds);
        public static implicit operator ProxyCredential(NetworkCredential icreds) => new(icreds);

        NetworkCredential? ICredentials.GetCredential(Uri uri, string authType) => _nc.GetCredential(uri, authType);
    }
}
