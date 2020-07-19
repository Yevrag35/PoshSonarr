//using MG.Sonarr.Functionality.Extensions;
using MG.Api.Json;
using MG.Api.Rest.Generic;
using MG.Sonarr.Functionality.Internal;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality
{
    public static class ClassFactory
    {
        public static ISonarrUrl GenerateSonarrUrl(Uri url, bool includeApiPrefix) => new SonarrUrl(url, includeApiPrefix);
        public static ISonarrUrl GenerateSonarrUrl(string hostName, int portNumber, bool useSsl, string reverseProxyBase, bool includeApiPrefix)
        {
            return new SonarrUrl(hostName, portNumber, useSsl, reverseProxyBase, includeApiPrefix);
        }
        public static IEqualityComparer<string> NewIgnoreCase() => new IgnoreCase();
        public static IJobHistory NewJobHistory() => new JobHistoryRepository();
        public static ITagManager NewTagManager(ISonarrClient client, bool addApiToPath) => new TagManager(client, addApiToPath);
    }
}
