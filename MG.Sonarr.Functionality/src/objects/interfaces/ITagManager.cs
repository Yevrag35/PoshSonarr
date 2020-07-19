using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    public interface ITagManager : IDisposable
    {
        ITagCollection AllTags { get; }
        string Endpoint { get; }

        int AddNew(string label);
        Tag Edit(int id, string newLabel);
        bool Remove(int id);

        bool Exists(int tagId);
        bool Exists(string label, StringComparison comparison);

        int GetId(string label, StringComparison comparison);
        string GetLabel(int id);

        Tag GetTag(int id);
        Tag GetTag(object idOrLabel);
        Tag GetTag(string label, StringComparison comparison);

        IEnumerable<Tag> GetTags(IEnumerable<int> ids);
        IEnumerable<Tag> GetTags(IEnumerable<string> labels);
        List<Tag> GetTags(IEnumerable<object> possibles, out HashSet<string> nonMatchingStrs, out HashSet<int> nonMatchingIds);

        bool TryGetId(string label, out int tagId);
        bool TryGetLabel(int id, out string label);
        bool TryGetTag(string label, StringComparison Comparison, out Tag tag);
        bool TryGetTag(int id, out Tag tag);
        bool TryGetTag(object tagObj, out Tag tag);
    }
}
