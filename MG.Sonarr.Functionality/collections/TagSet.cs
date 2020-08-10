using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality.Collections
{
    public class TagSet : IReadOnlyCollection<Tag>
    {
        private HashSet<Tag> _set;

        public TagSet(IEnumerable<Tag> tags) => _set = new HashSet<Tag>(tags);

        private class TagEquality : IEqualityComparer<Tag>
        {
            public bool Equals(Tag x, Tag y)
            {
                return x.Id.Equals(y.Id);
            }
        }
    }
}
