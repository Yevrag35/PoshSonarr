using MG.Sonarr.Results;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Internal
{
    internal class PastJob : IPastJob, IComparable<IPastJob>, IComparable<PastJob>
    {
        public string Command { get; }
        public DateTimeOffset Ended { get; }
        public long Id { get; }
        public DateTimeOffset Started { get; }

        internal PastJob(CommandResult output)
        {
            //this.Order = order;
            this.Command = output.CommandName;
            this.Id = output.JobId;
            this.Ended = output.Ended.GetValueOrDefault();
            this.Started = output.Started.Value;
        }

        int IComparable<PastJob>.CompareTo(PastJob other) => this.CompareTo(other);
        public int CompareTo(IPastJob other)
        {
            int start = this.Started.CompareTo(other.Started) * -1;
            if (start == 0)
            {
                int ended = this.Ended.CompareTo(other.Ended) * -1;
                if (ended == 0)
                {
                    return this.Command.CompareTo(other.Command);
                }
                else
                    return ended;
            }
            else
                return start;
        }

        [Obsolete]
        internal static bool TryFromResult(CommandResult result, out IPastJob pastJob)
        {
            pastJob = null;
            if (result != null && result.Status == CommandStatus.Completed && result.Started.HasValue)
            {
                pastJob = new PastJob(result);
            }

            return pastJob != null;
        }
    }
}
