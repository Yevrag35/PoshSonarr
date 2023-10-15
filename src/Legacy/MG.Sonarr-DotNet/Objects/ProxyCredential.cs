using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Reflection;

namespace MG.Sonarr
{
    /// <summary>
    /// A mock <see cref="NetworkCredential"/> class that can implicitly convert <see cref="PSCredential"/> to <see cref="ICredentials"/>.
    /// </summary>
    public class ProxyCredential : ICredentials
    {
        #region FIELDS/CONSTANTS
        private ICredentials _nc;

        #endregion

        #region CONSTRUCTORS
        private ProxyCredential(ICredentials icreds) => _nc = icreds;
        private ProxyCredential(PSCredential psCreds)
            : this(psCreds.GetNetworkCredential()) { }

        #endregion

        #region METHODS
        public static implicit operator ProxyCredential(PSCredential psCreds) => new ProxyCredential(psCreds);
        public static implicit operator ProxyCredential(NetworkCredential icreds) => new ProxyCredential(icreds);

        NetworkCredential ICredentials.GetCredential(Uri uri, string authType) => _nc.GetCredential(uri, authType);

        #endregion
    }
}