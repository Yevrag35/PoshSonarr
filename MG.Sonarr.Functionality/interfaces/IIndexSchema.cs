using MG.Sonarr.Results;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    public interface IIndexerSchema : IIndexer
    {
        IReadOnlyList<IIndexer> Presets { get; }
    }
}
