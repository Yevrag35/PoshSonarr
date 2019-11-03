using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public static class ClassFactory
    {
        public static IComparer<LogFile> GenerateLogFileComparer() => new LogFile.LogFileSortById();
        public static ISonarrUrl GenerateSonarrUrl(Uri url, bool includeApiPrefix) => new SonarrUrl(url, includeApiPrefix);
        public static ISonarrUrl GenerateSonarrUrl(string hostName, int portNumber, bool useSsl, string reverseProxyBase, bool includeApiPrefix)
        {
            return new SonarrUrl(hostName, portNumber, useSsl, reverseProxyBase, includeApiPrefix);
        }
        public static IEqualityComparer<string> NewIgnoreCase() => new IgnoreCase();
    }
}
