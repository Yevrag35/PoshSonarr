using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality.Jobs
{
    internal class JobHistoryRepository : IEnumerable<PastJob>, IJobHistory
    {
        private List<PastJob> _jobs;

        public int Count => _jobs.Count;
        public long[] Ids => _jobs.Count > 0 ? _jobs.Select(x => x.Id).OrderBy(id => id).ToArray() : null;

        internal JobHistoryRepository() => _jobs = new List<PastJob>();

        private void AddResultInternal(ICommandOutput newEntry)
        {
            if (this.ResultIsValid(newEntry) && !_jobs.Exists(x => x.Id == newEntry.Id))
            {
                _jobs.Add(new PastJob(newEntry));
            }
        }
        public void AddResult(ICommandOutput newEntry)
        {
            this.AddResultInternal(newEntry);
            this.Sort();
        }
        public void AddResults(IEnumerable<ICommandOutput> entries)
        {
            foreach (ICommandOutput cr in entries) {
                this.AddResultInternal(cr);
            }
            this.Sort();
        }

        public IEnumerator<IPastJob> GetEnumerator()
        {
            foreach (IPastJob ipj in _jobs)
            {
                yield return ipj;
            }
        }
        IEnumerator<PastJob> IEnumerable<PastJob>.GetEnumerator() => _jobs.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _jobs.GetEnumerator();

        public IEnumerable<IPastJob> FindById(IEnumerable<long> ids)
        {
            if (ids == null || _jobs.Count <= 0)
                return null;

            return _jobs.FindAll(x => ids.Contains(x.Id));
        }

        public void Sort() => _jobs.Sort();

        private bool ResultIsValid(ICommandOutput result) => result != null && result.Started.HasValue;
        public void UpdateRecord(ICommandResult result) => _jobs.Find(x => x.Id == result.Id)?.Update(result);
    }
}
