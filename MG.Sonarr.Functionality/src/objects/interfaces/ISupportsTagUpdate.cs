using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    public interface ISupportsTagUpdate : IGetEndpoint, IJsonResult
    {
        object Identifier { get; }
        HashSet<int> Tags { get; set; }

        //void AddTags(IEnumerable<int> tagIds);
        //void AddTags(params int[] tagIds);
        //void RemoveTags(IEnumerable<int> tagIds);
        //void RemoveTags(params int[] tagIds);
    }
}
