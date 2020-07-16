using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality.Internal
{
    internal class JobHistoryRepository : IEnumerable<PastJob>, IJobHistory
    {
        private List<PastJob> _jobs;

        public int Count => _jobs.Count;

        internal JobHistoryRepository() => _jobs = new List<PastJob>();

        private JobHistoryRepository(CommandResult single)
        {
            _jobs = new List<PastJob>(1);
            PastJob pj = new PastJob(single);
            _jobs.Add(pj);
        }
        private JobHistoryRepository(IEnumerable<CommandResult> results)
        {
            _jobs = new List<PastJob>(results
                .Where(x => this.ResultIsValid(x))
                .Select(x => new PastJob(x))
                    .OrderByDescending(x => x.Started)
                    .ThenByDescending(x => x.Ended)
            );
        }

        private void AddResultInternal(CommandResult newEntry)
        {
            if (!_jobs.Exists(x => x.Id == newEntry.JobId))
            {
                _jobs.Add(new PastJob(newEntry));
            }
        }
        public void AddResult(CommandResult newEntry)
        {
            this.AddResultInternal(newEntry);
            this.Sort();
        }
        public void AddResults(IEnumerable<CommandResult> entries)
        {
            foreach (CommandResult cr in entries) {
                this.AddResultInternal(cr);
            }
            this.Sort();
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

        public void Sort() => _jobs.Sort();

        private bool ResultIsValid(CommandResult result) => result != null && result.Status == CommandStatus.Completed && result.Started.HasValue;
    }
}
