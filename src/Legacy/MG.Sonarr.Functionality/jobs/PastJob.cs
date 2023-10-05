using MG.Sonarr.Results;
using System;

namespace MG.Sonarr.Functionality.Jobs
{
    internal class PastJob : IPastJob, IComparable<IPastJob>, IComparable<PastJob>
    {
        public string Command { get; } = string.Empty;
        public DateTimeOffset? Ended { get; private set; }
        public long Id { get; }
        public DateTimeOffset Started { get; }
        public string Status { get; private set; } = string.Empty;

        internal PastJob(ICommandOutput output)
        {
            this.Command = output.Command;
            this.Id = output.Id;
            this.Started = output.Started.Value;
            this.Status = output.Status;

            if (output is ICommandResult cr)
            {
                this.Ended = cr.Ended.GetValueOrDefault();
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
            if (result != null && result.Status.Equals("Completed", StringComparison.InvariantCultureIgnoreCase) && result.Started.HasValue)
            {
                pastJob = new PastJob(result);
            }

            return pastJob != null;
        }

        internal void Update(ICommandResult icr)
        {
            if (this.Id == icr.Id)
            {
                if (!this.Ended.HasValue && icr.Ended.HasValue)
                {
                    this.Ended = icr.Ended;
                }
                if (this.Status != icr.Status)
                {
                    this.Status = icr.Status;
                }
            }
        }
    }
}
