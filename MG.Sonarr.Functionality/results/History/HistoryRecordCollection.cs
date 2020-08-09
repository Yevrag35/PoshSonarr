using MG.Sonarr.Functionality.Results;
using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Result
{
    public class HistoryRecordCollection : ResultListBase<HistoryRecord>, IReadOnlyList<IRecord>
    {
    }
}
