using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Generic
{
    public abstract class ValidatedString : IEnumerable<string>
    {
        internal abstract string Value { get; }

        public override string ToString() => Value;

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
    }
}
