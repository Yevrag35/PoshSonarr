using System;
using System.Collections.Generic;
using System.Net;

namespace MG.Sonarr
{
    public interface IApiKey
    {
        string Key { get; }
        KeyValuePair<string, string> AsKeyValuePair();
    }
}
