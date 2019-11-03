using MG.Sonarr.Functionality.Extensions;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace MG.Sonarr.Functionality
{
    public static class ClassFactory
    {
        public static IRestResponse<T> CreateResponse<T>(HttpResponseMessage response)
        {
            var sonarrResponse = new SonarrRestResponse<T>();
            using (response)
            {
                sonarrResponse.StatusCode = response.StatusCode;
                try
                {
                    using (var content = response.Content)
                    {
                        sonarrResponse.Result = content.ReadAsJsonAsync<T>().GetAwaiter().GetResult();
                    }
                }
                catch (Exception e)
                {
                    sonarrResponse.Exception = e;
                }
            }
            return sonarrResponse;
        }
        public static IComparer<LogFile> GenerateLogFileComparer() => new LogFile.LogFileSortById();
        public static ISonarrUrl GenerateSonarrUrl(Uri url, bool includeApiPrefix) => new SonarrUrl(url, includeApiPrefix);
        public static ISonarrUrl GenerateSonarrUrl(string hostName, int portNumber, bool useSsl, string reverseProxyBase, bool includeApiPrefix)
        {
            return new SonarrUrl(hostName, portNumber, useSsl, reverseProxyBase, includeApiPrefix);
        }
        public static IEqualityComparer<string> NewIgnoreCase() => new IgnoreCase();
    }
}
