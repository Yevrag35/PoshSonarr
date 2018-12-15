using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sonarr.Api.Endpoints
{
    public interface ISonarrPostable : ISonarrEndpoint
    {
        Dictionary<string, object> Parameters { get; }
        string GetPostBody();
    }
}
