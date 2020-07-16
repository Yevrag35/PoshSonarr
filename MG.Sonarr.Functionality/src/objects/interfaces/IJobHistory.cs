using MG.Sonarr.Results;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    public interface IJobHistory : IEnumerable<IPastJob>
    {
        int Count { get; }
        void AddResult(CommandResult newEntry);
        void AddResults(IEnumerable<CommandResult> newEntries);

        void Sort();
    }
}
