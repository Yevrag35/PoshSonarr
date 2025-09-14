using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Metadata
{
    public sealed partial class MetadataList<T>
    {
        /// <inheritdoc/>
        [DebuggerStepThrough]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        /// <inheritdoc/>
        [DebuggerStepThrough]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="Enumerator"/> that can be used to iterate through the collection.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// An ernumerator for the elements of a <see cref="MetadataList{T}"/> collection.
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        public struct Enumerator : IEnumerator<T>
        {
            private readonly MetadataList<T> _list;
            private int _index;

            /// <inheritdoc/>
            public readonly T Current => _list._list[_index];

            /// <inheritdoc/>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            readonly object? IEnumerator.Current => this.Current;

            /// <summary>
            /// Initializes a new instance of the Enumerator class for the specified MetadataList.
            /// </summary>
            /// <param name="list">The MetadataList to enumerate over.</param>
            internal Enumerator(MetadataList<T> list)
            {
                _list = list;
                _index = -1;
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                int index = _index + 1;
                if ((uint)index < (uint)_list._list.Count)
                {
                    _index = index;
                    return true;
                }

                _index = _list._list.Count;
                return false;
            }

            /// <inheritdoc/>
            [DebuggerStepThrough]
            readonly void IDisposable.Dispose()
            {
                // Nothing to dispose
            }
            /// <inheritdoc/>
            [DebuggerStepThrough]
            void IEnumerator.Reset()
            {
                _index = -1;
            }
        }
    }
}
