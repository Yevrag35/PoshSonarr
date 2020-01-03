using System;

namespace MG.Sonarr.Functionality
{
    public interface ISupportsTagUpdate
    {
        object Identifier { get; }
        int[] Tags { get; }

        void AddTags(params int[] tagIds);
        void RemoveTags(params int[] tagIds);
        string GetEndpoint();
    }
}
