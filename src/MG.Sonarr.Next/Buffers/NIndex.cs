using System.Numerics;

namespace MG.Sonarr.Next.Buffers;

/// <summary>
/// Represents an index value that is positive up to <see cref="int.MaxValue"/> when initialized or -1 when invalid.
/// </summary>
/// <remarks>
/// This is an experimental type that is intended to be used in place of <see cref="int"/> for index values in enumerators.
/// The size of this type is 4 bytes.
/// </remarks>
[StructLayout(LayoutKind.Sequential), DebuggerDisplay(@"{_data} \{Valid = {IsValid}\}")]
//[JsonConverter(typeof(NIndexConverter))]
public readonly partial struct NIndex :
	IComparable<NIndex>, IComparable<int>, IComparable<uint>,
	IEquatable<NIndex>, IEquatable<int>, IEquatable<uint>,
	IEqualityOperators<NIndex, NIndex, bool>, IEqualityOperators<NIndex, int, bool>, IEqualityOperators<NIndex, uint, bool>,
	IMinMaxValue<NIndex>,
	ISpanFormattable, IUtf8SpanFormattable
{
	/// <summary>
	/// The index value.
	/// </summary>
	private readonly int _data;

	/// <summary>
	/// Indicates whether this index is valid (non-negative).
	/// </summary>
	public bool IsValid
	{
		[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _data >= 0;
	}
	/// <summary>
	/// Indicates whether this index is invalid (-1).
	/// </summary>
	public bool IsInvalid
	{
		[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _data < 0;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="NIndex"/> marking it as invalid (-1).
	/// </summary>
	[DebuggerStepThrough]
	public NIndex()
		: this(-1, isUnchecked: true)
	{
	}
	[DebuggerStepThrough]
	private NIndex(int value)
	{
		int mask = value >> 31;
		_data = value | mask;
	}
	[DebuggerStepThrough]
	private NIndex(uint value)
		: this((int)(value & int.MaxValue))
	{
	}
	[DebuggerStepThrough]
	private NIndex(int value, bool isUnchecked)
	{
		Debug.Assert(isUnchecked, "This constructor should only be used when the value is guaranteed to be valid.");
		Debug.Assert(value >= -1, "The value must be greater than or equal to -1.");
		// This constructor is used to create an NIndex from an int without validation.
		// It is intended for internal use only, hence the private access modifier.
		// The value is stored directly without any checks.
		_data = value;
	}

	/// <summary>
	/// Increments the index value by one.
	/// </summary>
	/// <returns>A new <see cref="NIndex"/> instance with the value of this instance incremented by one.</returns>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly NIndex Increment()
	{
		return new(_data + 1, isUnchecked: true);
	}

	/// <summary>
	/// Retrieves a reference to the item in the given <see cref="List{T}"/> at the index position represented
	/// by this instance.
	/// </summary>
	/// <remarks>
	/// Unlike the unsafe version, this method will throw an <see cref="IndexOutOfRangeException"/> if this index
	/// is invalid or out of bounds.
	/// </remarks>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="items">The list of items to retrieve the item from.</param>
	/// <returns>
	/// A read-only reference to the item in the list at the index position represented by this instance.
	/// </returns>
	/// <exception cref="IndexOutOfRangeException">This index value is invalid (-1) -or- greater than or equal to the number of items in <paramref name="items"/>.</exception>
	public ref readonly T GetItemRef<T>(List<T> items)
	{
		ReadOnlySpan<T> span = CollectionsMarshal.AsSpan(items);
		return ref span[_data];
	}
	/// <summary>
	/// Retrieves a reference to the item in the given <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> at the index position 
	/// represented by this instance.
	/// </summary>
	/// <remarks>
	/// Unlike the unsafe version, this method will return a <see langword="null"/> reference if this index is invalid or out of bounds.
	/// </remarks>
	/// <typeparam name="T">The type of the items in the read-only span.</typeparam>
	/// <param name="span">The read-only span to retrieve the value from.</param>
	/// <returns>
	/// A read-only reference to the item in the span at the index position represented by this instance.
	/// </returns>
	public ref readonly char GetItemRefChar(ReadOnlySpan<char> span)
	{
		return ref (uint)_data < (uint)span.Length
			? ref Unsafe.Add(ref MemoryMarshal.GetReference(span), _data)
			: ref Unsafe.NullRef<char>();
	}
	public ref readonly T GetItemRef<T>(T[] array)
	{
		return ref (uint)_data < (uint)array.Length
			 ? ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), _data)
			 : ref Unsafe.NullRef<T>();
	}
	/// <summary>
	/// Retrieves a reference to the item in the given <see cref="ReadOnlySpan{T}"/> of <typeparamref name="T"/> at the index position 
	/// represented by this instance.
	/// </summary>
	/// <remarks>
	/// Unlike the unsafe version, this method will return a <see langword="null"/> reference if this index is invalid or out of bounds.
	/// </remarks>
	/// <typeparam name="T">The type of the items in the read-only span.</typeparam>
	/// <param name="span">The read-only span to retrieve the value from.</param>
	/// <returns>
	/// A read-only reference to the item in the span at the index position represented by this instance.
	/// </returns>
	public ref readonly T GetItemRef<T>(ReadOnlySpan<T> span)
	{
		return ref (uint)_data < (uint)span.Length
			? ref Unsafe.Add(ref MemoryMarshal.GetReference(span), _data)
			: ref Unsafe.NullRef<T>();
	}
	/// <summary>
	/// Retrieves a reference to the item in the given <see cref="List{T}"/> at the index position represented
	/// by this instance.
	/// </summary>
	/// <remarks>
	/// No validation is performed on the index value. This method is intended for consumption by methods which
	/// have previously guaranteed the index value is valid.
	/// </remarks>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="items">The list of items to retrieve the item from.</param>
	/// <returns>A read-only reference to the item in the list at the index position represented by this instance.</returns>
	public unsafe ref readonly T GetItemRefUnsafe<T>(List<T> items)
	{
		Debug.Assert(_data >= 0, "The index value is invalid (-1).");

		ReadOnlySpan<T> span = CollectionsMarshal.AsSpan(items);
		ref T item = ref MemoryMarshal.GetReference(span);
		return ref Unsafe.Add(ref item, _data);
	}
	/// <summary>
	/// Retrieves a reference to the item in the given read-only span at the index position represented
	/// by this instance.
	/// </summary>
	/// <remarks>
	/// No validation is performed on the index value. This method is intended for consumption by methods which
	/// have previously guaranteed the index value is valid.
	/// </remarks>
	/// <typeparam name="T">The type of the items in the span.</typeparam>
	/// <param name="span">The read-only span of values to retrieve the item from.</param>
	/// <returns>A read-only reference to the value in the span at the index position represented by this instance.</returns>
	public unsafe ref readonly T GetItemRefUnsafe<T>(ReadOnlySpan<T> span)
	{
		Debug.Assert(_data >= 0, "The index value is invalid (-1).");
		ref T item = ref MemoryMarshal.GetReference(span);
		return ref Unsafe.Add(ref item, _data);
	}

	public unsafe ref T GetMutableItemRefUnsafe<T>(ReadOnlySpan<T> span)
	{
		Debug.Assert(_data >= 0, "The index value is invalid (-1).");
		ref T item = ref MemoryMarshal.GetReference(span);
		return ref Unsafe.Add(ref item, _data);
	}

	/// <summary>
	/// If <paramref name="useIncrement"/> is <c>true</c>, returns <c>this.Increment()</c>;
	/// otherwise returns an <see cref="NIndex"/> whose internal value is
	/// <see cref="int.MaxValue"/> — all without a conditional branch.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public NIndex IncrementOrMax(bool useIncrement)
	{
		// 1. Turn the bool into a mask that is either all‑zeros (0) or all‑ones (-1).
		//    Unsafe.As avoids the normal bool → int conversion helper, and the
		//    arithmetic negation ("-") flips 1 → -1 while leaving 0 unchanged.
		int mask = -Unsafe.As<bool, byte>(ref Unsafe.AsRef(in useIncrement));   // -1 if true, 0 if false

		// 2. Compute the "increment" candidate.  (Wrap‑around on int.MaxValue is OK
		//    because the private ctor below will coerce any negative input to -1.)
		int inc = _data + 1;

		// 3. Pick one of the two candidates with the mask.
		//    When mask == -1  (all bits 1)  => result = inc
		//    When mask ==  0  (all bits 0)  => result = int.MaxValue
		int chosen = (inc & mask) | (int.MaxValue & ~mask);
		// equivalently:  int chosen = inc ^ ((inc ^ int.MaxValue) & ~mask);

		return new NIndex(chosen);    // private ctor already normalizes negatives
	}

	/// <summary>
	/// Throws an <see cref="ArgumentOutOfRangeException"/> if the index value is invalid.
	/// </summary>
	/// <param name="index">The index value to validate.</param>
	/// <param name="paramName">The name of the parameter that the index value represents.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is invalid.</exception>
	[DebuggerStepThrough]
	public static void ThrowIfInvalid(NIndex index, [CallerArgumentExpression(nameof(index))] string? paramName = null)
	{
		if (index._data < 0)
		{
			paramName ??= nameof(index);
			throw new ArgumentOutOfRangeException(paramName, "The index value is invalid.");
		}
	}
	/// <summary>
	/// Validates that the specified <paramref name="index"/> is within the valid range [0, <paramref name="upperBound"/>],
	/// and throws an <see cref="ArgumentOutOfRangeException"/> if the validation fails.
	/// </summary>
	/// <param name="index">The index to validate. Must be non-negative and less than <paramref name="upperBound"/>.</param>
	/// <param name="upperBound">The exclusive upper bound for the valid range. Must be greater than zero.</param>
	/// <param name="paramName">The name of the parameter being validated. This is automatically populated by the compiler when using the <see
	/// cref="CallerArgumentExpressionAttribute"/>.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is less than 0 or greater than or equal to <paramref name="upperBound"/>.</exception>
	[DebuggerStepThrough]
	public static void ThrowIfInvalidOrOutOfRange(NIndex index, int upperBound, [CallerArgumentExpression(nameof(index))] string? paramName = null)
	{
		if (index._data < 0 || index._data >= upperBound)
		{
			throw new ArgumentOutOfRangeException(paramName ?? nameof(index), index._data, $"The index value is out of range of the upper bound '{upperBound}'.");
		}
	}
	/// <summary>
	/// Validates the specified <paramref name="index"/> is within the valid range [0, <paramref name="upperBound"/>], and throws
	/// an <see cref="ArgumentOutOfRangeException"/> if the validation fails.
	/// </summary>
	/// <param name="index">The zero-based index to validate.</param>
	/// <param name="upperBound">The exclusive upper bound for the valid range. Must be greater than or equal to zero.</param>
	/// <param name="paramName">The name of the parameter being validated. This is automatically populated by the compiler when using the  <see
	/// cref="CallerArgumentExpressionAttribute"/>.</param>
	/// <returns>An <see cref="NIndex"/> instance representing the validated index.</returns>
	[DebuggerStepThrough]
	public static NIndex ThrowIfInvalidOrOutOfRange(int index, int upperBound, [CallerArgumentExpression(nameof(index))] string? paramName = null)
	{
		NIndex nin = new(index, isUnchecked: true); // Create NIndex without validation - we throw if invalid later.
		ThrowIfInvalidOrOutOfRange(nin, upperBound, paramName);

		return nin;
	}

	#region PUBLIC STATIC FIELDS
	/// <summary>
	/// The maximum value of <see cref="NIndex"/>.
	/// </summary>
	/// <value>
	/// <c>2,147,483,647</c>
	/// </value>
	private static readonly NIndex s_maxValue = new(int.MaxValue, isUnchecked: true);
	/// <summary>
	/// The minimum value of <see cref="NIndex"/>.
	/// </summary>
	/// <value>
	/// <c>-1</c>
	/// </value>
	public static readonly NIndex MinValue = new();

	#endregion

	#region INTERFACE IMPLEMENTATIONS

	/// <inheritdoc/>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	static NIndex IMinMaxValue<NIndex>.MaxValue
	{
		[DebuggerStepThrough]
		get => s_maxValue;
	}
	/// <inheritdoc/>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	static NIndex IMinMaxValue<NIndex>.MinValue
	{
		[DebuggerStepThrough]
		get => MinValue;
	}
	#endregion
}