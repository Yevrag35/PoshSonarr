using MG.Api.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    public interface IIndexer : IJsonObject
    {
        string ConfigContract { get; }
        IEnumerable<IField> Fields { get; }
        string Implementation { get; }
        string ImplementationName { get; }
        Uri InfoLink { get; }
        string Name { get; }
        string Protocol { get; }
        bool RssEnabled { get; set; }
        bool RssSupported { get; }
        bool SearchEnabled { get; set; }
        bool SearchSupported { get; }
    }
}
