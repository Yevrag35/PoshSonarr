using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Components;
public ref partial struct RentedBuffer<T>
{
	/// <summary>
	/// Enumerates the elements of a <see cref="RentedBuffer{T}"/>.
	/// </summary>
	[DebuggerStepThrough]
	[StructLayout(LayoutKind.Auto)]
	public ref struct Enumerator
	{
		private readonly ReadOnlySpan<T> _span;
		private int _index;

		/// <summary>
		/// Gets the current element in the collection.
		/// </summary>
		public readonly ref readonly T Current => ref _span[_index];

		/// <summary>
		/// Initializes the enumerator with the specified array.
		/// </summary>
		/// <param name="array">The array to enumerate.</param>
		internal Enumerator(ReadOnlySpan<T> array)
		{
			_span = array;
			_index = -1;
		}
		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// </summary>
		/// <returns>
		///		<see langword="true"/> if the enumerator was successfully advanced to the next element; 
		///		<see langword="false"/> if the enumerator has passed the end of the collection.
		/// </returns>
		public bool MoveNext()
		{
			int index = _index + 1;
			int length = _span.Length;
			if ((uint)index >= (uint)length)
			{
				_index = length;
				return false;
			}

			_index = index;
			return true;
		}
		/// <summary>
		/// Sets the enumerator to its initial position, which is before the first element in the collection.
		/// </summary>
		public void Reset()
		{
			_index = -1;
		}
	}
}
