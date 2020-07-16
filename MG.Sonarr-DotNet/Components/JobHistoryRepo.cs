using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MG.Sonarr.Components
{
    public static class JobHistoryRepo
    {
        private static HashSet<long> _jobRepo;

        public static void Initialize()
        {
            _jobRepo = new HashSet<long>();
        }

        internal static IEnumerable<bool> AddIdsToLog(IEnumerable<CommandOutput> outputs)
        {
            foreach (CommandOutput cmd in outputs)
            {
                yield return _jobRepo.Add(cmd.JobId);
            }
        }
        internal static bool AddIdToLog(CommandOutput cmdOutput) => _jobRepo.Add(cmdOutput.JobId);
        internal static bool AddIdToLog(long id) => _jobRepo.Add(id);

        public static long[] GetPastJobIds()
        {
            if (_jobRepo != null && _jobRepo.Count > 0)
            {
                return _jobRepo.ToArray();
            }
            else
                return null;
        }
    }
}
