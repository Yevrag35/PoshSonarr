using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;

namespace Sonarr.Api
{
    public interface ISonarrOperation
    {
        string OperationType { get; }
        //string 
    }

    public class Request
    {
        //public WebHeaderCollection Headers = new Dictionary<string, string>();

        //public WebRequest Create(ISonarrOperation operation)
        //{

        //}
    }
}
