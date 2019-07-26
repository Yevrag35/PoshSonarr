using System;
using System.Net;

namespace MG.Sonarr
{
    public interface IApiKey
    {
        string Key { get; }
        WebHeaderCollection AsSonarrHeader();
    }
}
