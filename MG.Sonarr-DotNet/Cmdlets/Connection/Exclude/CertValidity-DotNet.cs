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
using System.Security.Cryptography;

namespace MG.Sonarr
{
    internal static partial class Validation
    {
        internal static void CheckCertificateValidity(ref HttpClientHandler handler)
        {
            handler.ServerCertificateCustomValidationCallback = delegate { return true; };
        }
    }
}