using MG.Sonarr.Functionality;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    public interface ITagManager : IDisposable
    {
        ITagCollection AllTags { get; }
        string Endpoint { get; }
    }
}
