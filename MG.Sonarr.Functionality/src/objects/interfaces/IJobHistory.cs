using MG.Sonarr.Results;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    public interface IJobHistory : IEnumerable<IPastJob>
    {
        int Count { get; }
        long[] Ids { get; }

        void AddResult(ICommandOutput newEntry);
        void AddResults(IEnumerable<ICommandOutput> newEntries);

        IEnumerable<IPastJob> FindById(IEnumerable<long> ids);

        void UpdateRecord(ICommandResult result);
        void Sort();
    }
}
