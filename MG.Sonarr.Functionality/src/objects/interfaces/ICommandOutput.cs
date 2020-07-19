using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality
{
    public interface ICommandOutput
    {
        string Command { get; }
        long Id { get; }
        CommandStatus Status { get; }
        DateTimeOffset? Started { get; }
    }
}