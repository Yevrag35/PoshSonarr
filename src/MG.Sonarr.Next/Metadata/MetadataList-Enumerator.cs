namespace MG.Sonarr.Next.Metadata;

public sealed partial class MetadataList<T>
{
	/// <summary>
	/// Returns an enumerator over the list's elements.
	/// </summary>
	/// <returns>An enumerator over the elements, in order.</returns>
	[DebuggerStepThrough]
	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return _list.GetEnumerator();
	}
	/// <summary>
	/// Returns an enumerator over the list's elements.
	/// </summary>
	/// <returns>A boxed <see cref="Enumerator"/> over the elements, in order.</returns>
	[DebuggerStepThrough]
	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}
	/// <summary>
	/// Returns an enumerator over the list's elements.
	/// </summary>
	/// <returns>An <see cref="Enumerator"/> over the elements, in order.</returns>
	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	/// <summary>
	/// Provides an enumerator over the elements of a <see cref="MetadataList{T}"/>.
	/// </summary>
	/// <remarks>
	/// Unlike the enumerator of <see cref="List{T}"/>, it doesn't detect a change to the list during enumeration: it
	/// reads each element from the list's current contents and stops at its current count.
	/// </remarks>
	[StructLayout(LayoutKind.Auto)]
	public struct Enumerator : IEnumerator<T>
	{
		/// <summary>
		/// The list being enumerated.
		/// </summary>
		private MetadataList<T> _list;
		/// <summary>
		/// The index of <see cref="Current"/>: -1 before the first element, and the list's count after the last.
		/// </summary>
		private int _index;
		/// <summary>
		/// The element at <see cref="_index"/>.
		/// </summary>
		private T _current;

		/// <summary>
		/// Gets the element at the enumerator's position.
		/// </summary>
		/// <value>
		/// The current element, or <see langword="default"/> before the first call to <see cref="MoveNext"/>.
		/// </value>
		public readonly T Current => _current;

		/// <summary>
		/// Gets the element at the enumerator's position.
		/// </summary>
		/// <value>The current element, boxed when it's a value type.</value>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly object? IEnumerator.Current => this.Current;

		/// <summary>
		/// Initializes a new <see cref="Enumerator"/> instance, positioned before the first element of the specified list.
		/// </summary>
		/// <param name="list">The list to enumerate.</param>
		internal Enumerator(MetadataList<T> list)
		{
			_list = list;
			_index = -1;
			_current = default!;
		}

		/// <summary>
		/// Advances the enumerator to the next element.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the enumerator moved to an element; <see langword="false"/> if it passed the last.
		/// </returns>
		public bool MoveNext()
		{
			int index = _index + 1;
			if ((uint)index < (uint)_list._list.Count)
			{
				_index = index;
				_current = Unsafe.Add(ref MemoryMarshal.GetReference(_list.AsSpan()), index);
				return true;
			}

			_index = _list._list.Count;
			return false;
		}

		/// <summary>
		/// Clears the enumerator, which can't be used afterward.
		/// </summary>
		[DebuggerStepThrough]
		void IDisposable.Dispose()
		{
			this = default;
		}
		/// <summary>
		/// Moves the enumerator back to before the first element.
		/// </summary>
		[DebuggerStepThrough]
		void IEnumerator.Reset()
		{
			_index = -1;
			_current = default!;
		}
	}
}
