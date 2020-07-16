using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MG.Sonarr.Functionality.Internal
{
    internal class JobHistoryRepository : IEnumerable<PastJob>, IJobHistory
    {
        private List<PastJob> _jobs;

        public int Count => _jobs.Count;

        private JobHistoryRepository(CommandResult single)
        {
            _jobs = new List<PastJob>(1);
            PastJob pj = new PastJob(single, 0);
            _jobs.Add(pj);
        }
        private JobHistoryRepository(IEnumerable<CommandResult> results)
        {
            CommandResult[] orderedArray = results
                .Where(x => this.ResultIsValid(x))
                    .OrderByDescending(x => x.Started.GetValueOrDefault())
                        .ThenByDescending(x => x.Ended.GetValueOrDefault())
                            .ToArray();

            _jobs = new List<PastJob>(orderedArray.Length);
            for (int i = 0; i < orderedArray.Length; i++)
            {
                CommandResult cr = orderedArray[i];
                PastJob job = new PastJob(cr, i);
                _jobs.Add(job);
            }
        }

        public int Add(CommandResult newEntry)
        {
            if (this.ResultIsValid(newEntry))
            {
                int nextId = _jobs.Count;

            }
        }

        IEnumerator<IPastJob> IEnumerable<IPastJob>.GetEnumerator()
        {
            foreach (IPastJob ipj in _jobs)
            {
                yield return ipj;
            }
        }
        public IEnumerator<PastJob> GetEnumerator() => _jobs.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _jobs.GetEnumerator();

        private bool ResultIsValid(CommandResult result) => result != null && result.Status == CommandStatus.Completed && result.Started.HasValue;
    }
}
