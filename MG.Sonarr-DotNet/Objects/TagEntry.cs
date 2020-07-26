using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    public class TagEntry : IComparable<TagEntry>, IEquatable<TagEntry>
    {
        public TagAction Action { get; internal set; }
        public int Id { get; }

        public TagEntry(Tag tag, TagAction action)
        {
            this.Action = action;
            this.Id = tag.Id;
        }

        public int CompareTo(TagEntry other) => this.Id.CompareTo(other.Id);
        public bool Equals(TagEntry other) => this.Id.Equals(other.Id);
    }

    public class TagEntryEquality : IEqualityComparer<TagEntry>
    {
        public bool Equals(TagEntry x, TagEntry y) => x.Id.Equals(y.Id);
        public int GetHashCode(TagEntry entry) => entry.Id.GetHashCode();
    }

    public enum TagAction
    {
        Set,
        Add,
        Remove
    }
}
