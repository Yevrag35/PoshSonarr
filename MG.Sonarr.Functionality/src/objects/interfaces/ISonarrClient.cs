using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public interface ISonarrClient
    {
        bool IsAuthenticated { get; }

        void AddApiKey(IApiKey apiKey);
        bool IsJsonArray(string jsonString);

        //(bool, )
    }
}
