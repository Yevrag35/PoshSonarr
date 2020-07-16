using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MG.Sonarr
{
    public static class History
    {
        public static IJobHistory Jobs { get; private set; }

        public static void Initialize() => Jobs = ClassFactory.NewJobHistory();
    }
}
