using MG.Sonarr.Results;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    public interface IIndexSchema : IJsonResult
    {
        string ConfigContract { get; }
        IEnumerable<IField> Fields { get; }
        IReadOnlyList<IIndexer> Presets { get; }
    }
}
