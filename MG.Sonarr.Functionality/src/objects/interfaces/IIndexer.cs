using MG.Sonarr.Results;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    public interface IIndexer : IJsonResult
    {
        string ConfigContract { get; }
        IEnumerable<IField> Fields { get; }
        int Id { get; }
        string Implementation { get; }
        string ImplementationName { get; }
        Uri InfoLink { get; }
        string Name { get; }
        DownloadProtocol Protocol { get; }
        bool RssEnabled { get; set; }
        bool RssSupported { get; }
        bool SearchEnabled { get; set; }
        bool SearchSupported { get; }
    }
}
