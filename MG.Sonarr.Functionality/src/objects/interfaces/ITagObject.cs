using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    public interface IHasTagSet
    {
        HashSet<int> Tags { get; }
    }
}
