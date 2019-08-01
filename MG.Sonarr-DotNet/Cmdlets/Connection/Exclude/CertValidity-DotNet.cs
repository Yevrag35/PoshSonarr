using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Cmdlets
{
    public partial class ConnectInstance
    {
        private void CheckCertificateValidity(ref HttpClientHandler handler)
        {
            if (_skipCert)
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        }
    }
}