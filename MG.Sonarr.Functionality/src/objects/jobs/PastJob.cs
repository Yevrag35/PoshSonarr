using MG.Sonarr.Results;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Internal
{
    internal class PastJob : IPastJob, IComparable<IPastJob>, IComparable<PastJob>
    {
        public string Command { get; }
        public DateTimeOffset? Ended { get; private set; }
        public long Id { get; }
        public DateTimeOffset Started { get; }

        internal PastJob(ICommandOutput output)
        {
            this.Command = output.Command;
            this.Id = output.Id;
            this.Started = output.Started.Value;

            if (output is ICommandResult cr)
            {
                this.Ended = cr.Ended.GetValueOrDefault();
            }
        }

        public void SetEndTime(DateTimeOffset ended)
        {
            if (!this.Ended.HasValue)
            {
                this.Ended = ended;
            }
        }

        int IComparable<PastJob>.CompareTo(PastJob other) => this.CompareTo(other);
        public int CompareTo(IPastJob other)
        {
            int start = this.Started.CompareTo(other.Started) * -1;
            if (start == 0)
            {
                int ended = this.Ended.GetValueOrDefault().CompareTo(other.Ended.GetValueOrDefault()) * -1;
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
        internal static bool TryFromResult(CommandOutput result, out IPastJob pastJob)
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
