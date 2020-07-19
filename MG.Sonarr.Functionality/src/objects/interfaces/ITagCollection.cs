using MG.Sonarr.Results;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    public interface ITagCollection : IEnumerable<Tag>
    {
        Tag this[int index] { get; }

        bool Contains(int tagId);
        void Sort();
        void Sort(IComparer<Tag> comparer);
    }
}
