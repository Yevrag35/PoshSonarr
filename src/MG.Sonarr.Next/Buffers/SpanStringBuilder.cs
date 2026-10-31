using MG.Sonarr.Next.Guarding;
using MG.Sonarr.Next.Unions;
using System.ComponentModel;
using System.Numerics;

namespace MG.Sonarr.Next.Buffers;

/// <summary>
/// A ref struct that provides a way to build strings without allocating memory on the heap until the <see cref="string"/> is constructed.
/// </summary>
/// <remarks>
/// <para>The <see cref="SpanStringBuilder"/> utilizes stack-allocated buffers and pooled arrays to avoid or reuse heap allocations during the building process.</para>
/// <para>A span or reference into the buffer, such as the one that <see cref="AsSpan()"/>, <see cref="AppendSpan(int)"/>, or the
/// indexer returns, is valid only until the next call that grows the buffer, and it isn't valid as that call's argument
/// either. Growing returns a rented array to the pool before the growing call reads its argument, so a span over the builder
/// that's passed back to one of its own appends can read an array that the pool has already cleared or rented out again.
/// <see cref="Insert(int, ReadOnlySpan{char})"/> doesn't take a span over the builder at all, since it moves the characters
/// that such a span covers before it reads them.</para>
/// <para>In Debug builds, the calls that write a span of characters assert that it doesn't point into the buffer whenever
/// they grow the buffer, and <see cref="Insert(int, ReadOnlySpan{char})"/> asserts it on every call. A Debug build also fills
/// an array that goes back to the pool with <c>'\uDDDD'</c> characters, unless <see cref="ClearOnDispose"/> is set.</para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
[StructLayout(LayoutKind.Auto), DebuggerStepThrough]
internal ref struct SpanStringBuilder
{
	/// <summary>
	/// Creates a new <see cref="SpanStringBuilder"/> instance from a <see cref="RefEither{T1, T2}"/> that can either be a <see cref="Span{T}"/> of characters or an integer representing the desired capacity.
	/// </summary>
	/// <param name="either">A union that contains either a <see cref="Span{T}"/> of characters or an integer representing the desired capacity.</param>
	/// <returns>A new <see cref="SpanStringBuilder"/> instance.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SpanStringBuilder Create(RefEither<Span<char>, int> either)
	{
		return either.IsT1 ? new(either.AsT1) : rentFromLength(either.AsT2);

		[MethodImpl(MethodImplOptions.NoInlining)]
		static SpanStringBuilder rentFromLength(int length)
		{
			return new(length);
		}
	}

	internal const int DEFAULT_CAPACITY = 128;
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	static readonly string s_newLine = Environment.NewLine;
	static readonly int s_newLineLength = s_newLine.Length;

	private char[]? _array;
	private Span<char> _chars;
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private int _position;
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private bool _clearOnDispose;

	/// <summary>
	/// Implicitly converts a character span to a <see cref="SpanStringBuilder"/>.
	/// </summary>
	/// <remarks><para>The resulting <see cref="SpanStringBuilder"/> references the provided buffer directly. The
	/// buffer must remain valid for the lifetime of the returned instance.</para></remarks>
	/// <param name="buffer">The character buffer to wrap.</param>
	public static implicit operator SpanStringBuilder(Span<char> buffer)
	{
		return new SpanStringBuilder(buffer);
	}

	/// <summary>
	/// Gets a reference to the character at the specified index within the current written buffer.
	/// </summary>
	/// <param name="index">The zero-based index of the character.</param>
	/// <value>The reference to the character at the specified index.</value>
	/// <exception cref="ArgumentOutOfRangeException"/>
	public readonly ref char this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			ThrowHelper.ThrowIfNegativeOrGreaterThanOrEqualTo(index, (uint)_position);
			return ref _chars.Ref(index);
		}
	}

	/// <summary>
	/// Gets the total capacity of the current buffer.
	/// </summary>
	/// <value>
	/// The total number of characters that the <see cref="SpanStringBuilder"/> can currently store before resizing.
	/// </value>
	public readonly int Capacity => _chars.Length;
	/// <summary>
	/// Gets or sets a value indicating whether the buffer should be cleared when the <see cref="SpanStringBuilder"/> is disposed.
	/// </summary>
	/// <remarks>
	/// The value only has an effect while <see cref="IsRented"/> is <see langword="true"/>; a caller-provided buffer is
	/// never touched on disposal.
	/// </remarks>
	public bool ClearOnDispose
	{
		readonly get => _clearOnDispose;
		set => _clearOnDispose = value;
	}
	/// <summary>
	/// Indicates whether this <see cref="SpanStringBuilder"/> instance is default-initialized.
	/// </summary>
	[DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
	public readonly bool IsDefault => _chars.IsEmpty;
	/// <summary>
	/// Indicates whether this <see cref="SpanStringBuilder"/> instance is default-initialized or wraps a buffer of no
	/// characters.
	/// </summary>
	public readonly bool IsDefaultOrEmpty => _chars.IsEmpty;
	/// <summary>
	/// Gets a value indicating whether the internal buffer is rented from the <see cref="ArrayPool{T}"/>.
	/// </summary>
	/// <value><see langword="true"/> if the buffer is rented; otherwise, <see langword="false"/>.</value>
	public readonly bool IsRented => _array is not null;
	/// <summary>
	/// Gets the number of characters appended to the <see cref="SpanStringBuilder"/>.
	/// </summary>
	/// <value>The number of characters currently in the buffer.</value>
	public readonly int Length => _position;

	/// <summary>
	/// Initializes a new instance of the <see cref="SpanStringBuilder"/> struct with a specified initial capacity.
	/// </summary>
	/// <param name="minimumCapacity">The initial capacity for the buffer.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumCapacity"/> is negative.</exception>
	public SpanStringBuilder(int minimumCapacity)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(minimumCapacity);
		int capacity = Math.Max(DEFAULT_CAPACITY, minimumCapacity);
		capacity = BitHelper.RoundUpToPowerOf2(capacity);

		char[] array = Rent.Array<char>(capacity);
		_array = array;
		_chars = array.AsUnsafeSpan(0);
		_position = 0;
		_clearOnDispose = false;
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="SpanStringBuilder"/> struct with a pre-allocated buffer.
	/// </summary>
	/// <param name="initialBuffer">A span that provides the initial storage for the builder.</param>
	[DebuggerStepThrough]
	public SpanStringBuilder(Span<char> initialBuffer)
	{
		_array = null;
		_chars = initialBuffer;
		_position = 0;
		_clearOnDispose = false;
	}

	/// <summary>
	/// Allocates a new <see cref="string"/> from the characters appended to the builder and disposes this
	/// <see cref="SpanStringBuilder"/>.
	/// </summary>
	/// <remarks>
	///     Use <see cref="ToString"/> if you want to construct additional strings from the same builder.
	/// </remarks>
	/// <returns>
	///     The constructed <see cref="string"/> instance.
	/// </returns>
	public string Build()
	{
		string str = this.ToString();
		this.Dispose();
		return str;
	}

	/// <summary>
	/// Appends a character to the <see cref="SpanStringBuilder"/>.
	/// </summary>
	/// <param name="value">The character to append.</param>
	/// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Append(char value)
	{
		this.EnsureCapacity(new pint(1, true));
		_chars.Ref(_position++) = value;
	}
	/// <summary>
	/// Appends a specified number of copies of the given character to the <see cref="SpanStringBuilder"/>.
	/// </summary>
	/// <param name="value">The character to append.</param>
	/// <param name="count">The number of times to append the character.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
	/// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	public void Append(char value, int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		if (count == 0) return;

		this.EnsureCapacity(new pint(count, true));

		var span = _chars.UnsafeSlice(_position, count);
		if (count == 1)
		{
			span.Ref() = value;
		}
		else if (value == default)
		{
			span.Clear();
		}
		else
		{
			span.Fill(value);
		}

		_position += count;
	}
	/// <summary>
	/// Appends the string representation of the specified enumeration value to the current instance.
	/// </summary>
	/// <remarks>This method formats the enumeration value as a string and appends it to the builder.  If the
	/// enumeration value cannot be formatted within the initial buffer size, the buffer is resized dynamically.</remarks>
	/// <typeparam name="TEnum">The type of the enumeration. Must be an unmanaged enumeration type.</typeparam>
	/// <param name="enumValue">The enumeration value to append.</param>
	/// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	public void Append<TEnum>(TEnum enumValue) where TEnum : unmanaged, Enum
	{
		const int stack_cap = DEFAULT_CAPACITY / 2; // 64

		scoped Span<char> buffer = stackalloc char[stack_cap];
		char[]? rented = null;
		try
		{
			int written = 0;
			while (!Enum.TryFormat(enumValue, buffer, out written))
			{
				buffer = GrowScratch(ref rented, buffer.Length * 2);
			}

			ReadOnlySpan<char> slice = buffer.UnsafeSlice(0, written);
			this.Append(slice);
		}
		finally
		{
			Rent.Return(rented);
		}
	}
	///// <summary>
	///// Appends the string representation of the specified enumeration value to the current instance, formatting it
	///// through the supplied function pointer.
	///// </summary>
	///// <remarks>
	///// <para>Pass the address of the source-generated <c>TryFormat</c> member emitted for <typeparamref name="TEnum"/>
	///// - for example <c>&amp;TextVoiceExtensions.TryFormat</c>. That member resolves the name through a compile-time
	///// switch, whereas <see cref="Append{TEnum}(TEnum)"/> can only reach <c>Enum.TryFormat</c>, which searches
	///// per-type metadata on every call because a generic constrained to <see cref="Enum"/> cannot bind to a
	///// per-enum member.</para>
	///// <para>A default-initialized <paramref name="tryFormat"/> carries no pointer. That case routes to
	///// <see cref="Append{TEnum}(TEnum)"/> rather than throwing, so the overload stays safe to call with
	///// <see langword="default"/>.</para>
	///// </remarks>
	///// <typeparam name="TEnum">The type of the enumeration. Must be an unmanaged enumeration type.</typeparam>
	///// <param name="enumValue">The enumeration value to append.</param>
	///// <param name="tryFormat">A pointer to the function that writes <paramref name="enumValue"/> into a character buffer.</param>
	///// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	//public void Append<TEnum>(TEnum enumValue, TryFnPtr<TEnum, Span<char>, int> tryFormat) where TEnum : unmanaged, Enum
	//{
	//	// Guarding the local lets the JIT fold the wrapper's internal null branch away (RLT005).
	//	if (!tryFormat.IsValid)
	//	{
	//		this.Append(enumValue);
	//		return;
	//	}

	//	const int stack_cap = DEFAULT_CAPACITY / 2; // 64

	//	scoped Span<char> buffer = stackalloc char[stack_cap];
	//	char[]? rented = null;
	//	try
	//	{
	//		int written = 0;
	//		while (!tryFormat.Invoke(enumValue, buffer, out written))
	//		{
	//			buffer = GrowScratch(ref rented, buffer.Length * 2);
	//		}

	//		ReadOnlySpan<char> slice = buffer.UnsafeSlice(0, written);
	//		this.Append(slice);
	//	}
	//	finally
	//	{
	//		Rent.Return(rented);
	//	}
	//}
	/// <summary>
	/// Appends the string representation of the specified <see cref="Guid"/> to the current buffer.
	/// </summary>
	/// <remarks>The method ensures that the buffer has sufficient capacity to accommodate the appended
	/// value.</remarks>
	/// <param name="value">The <see cref="Guid"/> to append.</param>
	/// <param name="format">An optional format specifier that defines the format of the appended <see cref="Guid"/>.  If not specified, the
	/// default format is used.</param>
	/// <inheritdoc cref="LengthConstants.GetGuidLength(ReadOnlySpan{char}, string)" path="/exception"/>
	/// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	public void AppendGuid(Guid value, ReadOnlySpan<char> format = default)
	{
		int length = LengthConstants.GetGuidLength(format);
		this.EnsureCapacity(new pint(length, true));

		Span<char> slice = _chars.UnsafeSlice(_position);
		_ = value.TryFormat(slice, out int written, format);
		_position += written;

		Debug.Assert(!slice.UnsafeSlice(0, written).Contains(default), "This is where you're messing up!");
	}

	/// <summary>
	/// Appends the characters from the specified <see cref="ReadOnlySpan{T}"/> to the current instance.
	/// </summary>
	/// <param name="value">The span containing the characters to append.</param>
	/// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Append(scoped ReadOnlySpan<char> value)
	{
		if (!value.IsEmpty)
		{
			Debug.Assert(value.Length <= _chars.Length - _position || !value.Overlaps(_chars), "A span over the builder was passed to a call that grows its buffer.");
			this.EnsureCapacity(new pint(value.Length, true));
			this.AppendCore(value);
		}
	}

	/// <summary>
	/// Appends the characters from the specified <see cref="ReadOnlySpan{T}"/> to the current instance without checking capacity.
	/// </summary>
	/// <param name="value">The span containing the characters to append.</param>
	private void AppendCore(scoped ReadOnlySpan<char> value)
	{
		value.UnsafeCopy(_chars.UnsafeSlice(_position));
		_position += value.Length;
	}

	/// <summary>
	/// Appends the string representation of a specified <see cref="ISpanFormattable"/> value to this instance.
	/// </summary>
	/// <typeparam name="T">The type of <see cref="ISpanFormattable"/> being appended.</typeparam>
	/// <param name="formattable">The formattable value to append.</param>
	/// <param name="maxLength">The maximum length of the formatted string.</param>
	/// <param name="format">An optional format string.</param>
	/// <param name="provider">An optional format provider.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is negative or zero.</exception>
	/// <exception cref="FormatException">The value could not be formatted into the provided buffer.</exception>
	public void Append<T>(T formattable, int maxLength, ReadOnlySpan<char> format = default, IFormatProvider? provider = null) where T : ISpanFormattable
	{
		const int max_stack = 64;
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLength);

		if (maxLength <= max_stack)
		{
			Span<char> temp = stackalloc char[maxLength];
			if (!formattable.TryFormat(temp, out int written, format, provider))
			{
				ThrowFormatException(formattable, maxLength);
			}

			if (written > 0)
			{
				this.EnsureCapacity(unchecked((uint)written));
				this.AppendCore(temp.UnsafeSlice(0, written));
			}

			return;
		}

		this.AppendCore(formattable, maxLength, format, provider);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void AppendCore<T>(T formattable, int maxLength, ReadOnlySpan<char> format, IFormatProvider? provider) where T : ISpanFormattable
	{
		this.EnsureCapacity(unchecked((uint)maxLength));
		var slice = _chars.UnsafeSlice(_position, maxLength);
		if (!formattable.TryFormat(slice, out int written, format, provider))
		{
			ThrowFormatException(formattable, maxLength);
		}

		_position += written;
	}

	/// <summary>
	/// Throws a <see cref="FormatException"/> for a value that could not be formatted within the maximum length its
	/// caller allowed.
	/// </summary>
	/// <remarks>
	/// The message names the runtime type of <paramref name="formattable"/>, because <typeparamref name="T"/> is
	/// <see cref="ISpanFormattable"/> itself when a caller holds the value through the interface.
	/// </remarks>
	/// <typeparam name="T">The <see cref="ISpanFormattable"/> type that the caller holds <paramref name="formattable"/> as.</typeparam>
	/// <param name="formattable">The value that failed to format.</param>
	/// <param name="maxLength">The maximum number of characters that the value had to fit in.</param>
	/// <exception cref="FormatException">Thrown when this method is called.</exception>
	[DoesNotReturn, StackTraceHidden, DebuggerStepThrough, MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowFormatException<T>(T formattable, int maxLength) where T : ISpanFormattable
	{
		throw new FormatException($"The value of type '{formattable.GetType()}' could not be formatted into the provided buffer of length {maxLength}.");
	}

	/// <summary>
	/// Appends the result of the provided <see cref="Action{T, TState}"/> to the current instance.
	/// </summary>
	/// <typeparam name="T">The type of state object passed to the action.</typeparam>
	/// <param name="length">The number of characters to be written.</param>
	/// <param name="state">The state passed to the <paramref name="spanAction"/>.</param>
	/// <param name="spanAction">The action that writes to the span.</param>
	public void Append<T>(int length, scoped T state, Action<Span<char>, T> spanAction) where T : allows ref struct
	{
		this.EnsureCapacity(length);
		ref int pos = ref _position;
		spanAction(_chars.UnsafeSlice(pos, length), state);
		pos += length;
	}
	/// <summary>
	/// Appends the result of the provided <see cref="Action{T, TState}"/> to the current instance.
	/// </summary>
	/// <typeparam name="T">The type of state object passed to the action.</typeparam>
	/// <param name="length">The number of characters to be written.</param>
	/// <param name="state">The state passed to the <paramref name="spanAction"/>.</param>
	/// <param name="spanAction">The action that writes to the span.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="spanAction"/> is null.</exception>
	/// <inheritdoc cref="EnsureCapacity(int)" path="/exception"/>
	public
#if !NET11_0_OR_GREATER
		unsafe
#endif
		void Append<T>(int length, scoped T state, delegate*<Span<char>, T, void> spanAction) where T : allows ref struct
	{
		unsafe
		{
			ArgumentNullException.ThrowIfNull(spanAction);
			this.EnsureCapacity(length);
			spanAction(_chars.UnsafeSlice(_position, length), state);
		}

		_position += length;
	}
	/// <summary>
	/// Appends the result of the provided delegate to the current instance.
	/// </summary>
	/// <typeparam name="T">The type of state object passed to the function.</typeparam>
	/// <param name="maxLength">The maximum number of characters that may be written.</param>
	/// <param name="state">The state passed to the <paramref name="spanFunc"/>.</param>
	/// <param name="spanFunc">The delegate that writes to the span.</param>
	/// <inheritdoc cref="EnsureCapacity(int)" path="/exception"/>
	public void Append<T>(int maxLength, scoped T state, Func<Span<char>, T, int> spanFunc) where T : allows ref struct
	{
		this.EnsureCapacity(maxLength);
		int written = Math.Clamp(spanFunc(_chars.UnsafeSlice(_position, maxLength), state), 0, maxLength);
		_position += written;
	}
	/// <summary>
	/// Appends a formatted value to the current buffer using a user-provided callback function.
	/// </summary>
	/// <remarks>This method ensures that the buffer has sufficient capacity to accommodate <paramref
	/// name="maxLength"/> characters before invoking the callback function. The buffer's position is updated based on the
	/// number of characters written by the callback.</remarks>
	/// <typeparam name="T">The type of the state object passed to the callback function. Must be a ref struct.</typeparam>
	/// <param name="maxLength">The maximum number of characters that can be written to the buffer. Must be a positive value.</param>
	/// <param name="state">A state object that is passed to the callback function to provide additional context or data.</param>
	/// <param name="funcPtr">A pointer to a callback function that writes the formatted value to the provided <see cref="Span{T}"/>. The
	/// function must return the number of characters written.</param>
	/// <exception cref="ArgumentNullException"><paramref name="funcPtr"/> is null.</exception>
	/// <inheritdoc cref="EnsureCapacity(int)" path="/exception"/>
	[SuppressMessage("Style", "RLT013")]
	public
#if !NET11_0_OR_GREATER
		unsafe
#endif
		void Append<T>(int maxLength, scoped T state, delegate*<Span<char>, T, int> funcPtr) where T : allows ref struct
	{
		Span<char> slice;
		int written;
		unsafe
		{
			ArgumentNullException.ThrowIfNull(funcPtr);
			this.EnsureCapacity(maxLength);
			slice = _chars.UnsafeSlice(_position, maxLength);
			written = Math.Clamp(funcPtr(slice, state), 0, maxLength);
		}

		_position += written;
		Debug.Assert(!slice.Slice(0, written).Contains(default), "This is where you're messing up.");
	}
	/// <summary>
	/// Appends characters to the buffer by invoking a function pointer that writes to a preallocated span.
	/// </summary>
	/// <remarks><para>Ensures sufficient buffer capacity before invoking the function pointer. The function pointer
	/// receives a span of exactly <paramref name="maxLength"/> characters and must return the actual number of characters
	/// written. The internal position advances by the returned count, not by <paramref name="maxLength"/>.</para><para>The
	/// function pointer must write contiguous character data without gaps or <see langword="default"/> values in the
	/// written range.</para><para><b>Performance:</b> Uses unmanaged function pointers to avoid delegate allocation
	/// overhead.</para></remarks>
	/// <typeparam name="T">The type of the state data passed by reference to the function pointer.</typeparam>
	/// <param name="maxLength">The maximum number of characters that can be written by the function pointer.</param>
	/// <param name="state">A scoped reference to state data passed to the function pointer.</param>
	/// <param name="funcPtr">A function pointer that receives a character span and state reference, writes characters, and returns the number of
	/// characters written.</param>
	/// <inheritdoc cref="EnsureCapacity(int)" path="/exception"/>
	[SuppressMessage("Style", "RLT013")]
	internal
#if !NET11_0_OR_GREATER
		unsafe
#endif
		void Append<T>(int maxLength, scoped ref T state, delegate*<Span<char>, ref T, int> funcPtr) where T : allows ref struct
	{
		if (funcPtr is null)
			return;

		this.EnsureCapacity(maxLength);
		Span<char> slice = _chars.UnsafeSlice(_position, maxLength);
		
		int written;
		unsafe
		{
			written = Math.Clamp(funcPtr(slice, ref state), 0, maxLength);
		}

		_position += written;

		Debug.Assert(!slice.Slice(0, written).Contains(default), "This is where you're messing up.");
	}

	/// <summary>
	/// Appends UTF-8 encoded text to the <see cref="SpanStringBuilder"/>.
	/// </summary>
	/// <param name="utf8Text">The UTF-8 encoded bytes to append.</param>
	/// <inheritdoc cref="EnsureCapacity(int)" path="/exception"/>
	public void Append(ReadOnlySpan<byte> utf8Text)
	{
		if (utf8Text.IsEmpty)
		{
			return;
		}

		int maxLength = Encoding.UTF8.GetMaxCharCount(utf8Text.Length);
		this.EnsureCapacity(unchecked((uint)maxLength));

		ref char pos = ref _chars.Ref(_position);
		var span = pos.UnsafeSpan(maxLength);

		_position += Encoding.UTF8.GetChars(utf8Text, span);
	}
	/// <summary>
	/// Appends the specified spans of characters to the current instance.
	/// </summary>
	/// <remarks>This method allows appending multiple spans of characters in a single call.  The operation is
	/// performed efficiently using inlining to minimize overhead.</remarks>
	/// <param name="values">The characters to append, in order.</param>
	/// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	public void AppendChars(params ReadOnlySpan<char> values)
	{
		this.Append(values);
	}
	/// <summary>
	/// Appends the specified span of characters to the current instance, enclosing it with the specified character.
	/// </summary>
	/// <param name="value">The span of characters to append.</param>
	/// <param name="enclosedBy">The character to enclose the span with.</param>
	/// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	public void AppendEnclosedIn(ReadOnlySpan<char> value, char enclosedBy)
	{
		if (!value.IsEmpty)
		{
			int len = value.Length + 2;
			Debug.Assert(len <= _chars.Length - _position || !value.Overlaps(_chars), "A span over the builder was passed to a call that grows its buffer.");
			this.EnsureCapacity(new pint(len, true));
			var span = _chars.UnsafeSlice(_position, len);
			_position += len;

			span.Ref() = enclosedBy;
			value.UnsafeCopy(span.UnsafeSlice(1));
			span.Ref(span.Length - 1) = enclosedBy;
		}
	}
	/// <summary>
	/// Appends a new line to the <see cref="SpanStringBuilder"/>.
	/// </summary>
	/// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	public void AppendLine()
	{
		this.EnsureCapacity(new pint(s_newLineLength, true));
		ref char pos = ref _chars.Ref(_position);
		var nlSpan = pos.UnsafeSpan(s_newLineLength);
		ref char c = ref MemoryMarshal.GetReference(s_newLine.AsSpan());
		MemoryMarshal.CreateReadOnlySpan(ref c, s_newLineLength).UnsafeCopy(nlSpan);

		_position += s_newLineLength;
	}
	/// <summary>
	/// Appends a span of characters followed by a new line to the <see cref="SpanStringBuilder"/>.
	/// </summary>
	/// <param name="value">The span of characters to append.</param>
	/// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	public void AppendLine(scoped ReadOnlySpan<char> value)
	{
		int len = value.Length + s_newLineLength;
		Debug.Assert(len <= _chars.Length - _position || !value.Overlaps(_chars), "A span over the builder was passed to a call that grows its buffer.");
		this.EnsureCapacity(new pint(len, true));

		ref char pos = ref _chars.Ref(_position);
		var valueSpan = pos.UnsafeSpan(value.Length);
		value.UnsafeCopy(valueSpan);

		ref char newLinePos = ref pos.Offset(value.Length);
		var nlSpan = newLinePos.UnsafeSpan(s_newLineLength);

		s_newLine.AsSpan().UnsafeCopy(nlSpan);

		_position += len;
	}
	/// <summary>
	/// Appends a span of characters of the specified length to the current buffer.
	/// </summary>
	/// <param name="length">The number of characters to append. Must be non-negative and within the available capacity.</param>
	/// <returns>A <see cref="Span{T}"/> of characters representing the appended section of the buffer that can be written to.</returns>
	/// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<char> AppendSpan(int length)
	{
		Debug.Assert(length >= 0, "Length must be positive or 0.");
		if (length <= 0) return [];

		this.EnsureCapacity(new pint(length, true));
		var slice = _chars.UnsafeSlice(_position, length);

		_position += length;
		return slice;
	}

	/// <summary>
	/// Returns a span representing the current contents of the <see cref="SpanStringBuilder"/>.
	/// </summary>
	/// <remarks>
	/// The span is valid only until the next call that grows the buffer, and it isn't valid as that call's argument either.
	/// </remarks>
	/// <returns>A <see cref="ReadOnlySpan{T}"/> representing the characters in the builder.</returns>
	public readonly ReadOnlySpan<char> AsSpan()
	{
		return _position > 0
			? _chars.UnsafeSlice(0, _position)
			: [];
	}
	/// <summary>
	/// Returns a read-only span of characters from the underlying buffer, starting at the specified position and spanning
	/// the specified length.
	/// </summary>
	/// <remarks>
	/// The span is valid only until the next call that grows the buffer, and it isn't valid as that call's argument either.
	/// </remarks>
	/// <param name="start">The zero-based starting position of the span within the buffer.</param>
	/// <param name="length">The number of characters to include in the span.</param>
	/// <returns>A <see cref="ReadOnlySpan{T}"/> of characters representing the specified range of the buffer.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="start"/> or <paramref name="length"/> is
	/// negative, or when the range that they give ends past <see cref="Capacity"/>.</exception>
	public readonly ReadOnlySpan<char> AsSpan(int start, int length)
	{
		return _chars.Slice(start, length);
	}

	/// <summary>
	/// Removes the last character from the <see cref="SpanStringBuilder"/>.
	/// </summary>
	/// <remarks>
	/// The method does nothing when the builder is empty; it does not throw.
	/// </remarks>
	public void Backspace()
	{
		_position = Math.Max(_position - 1, 0);
	}
	/// <summary>
	/// Removes the last character from the <see cref="SpanStringBuilder"/> if it equals <paramref name="value"/>.
	/// </summary>
	/// <remarks>
	/// <para>The method does nothing when the builder is empty or ends with a different character; it does not
	/// throw.</para>
	/// <para>It removes at most one character, while <see cref="TrimEnd(char)"/> removes every trailing occurrence.</para>
	/// </remarks>
	/// <param name="value">The character to compare with the last character in the builder.</param>
	public void BackspaceIf(char value)
	{
		if (_position > 0 && _chars.Ref(_position - 1) == value)
		{
			_position--;
		}
	}

	/// <summary>
	/// Clears all data from the builder and resets the length to <c>0</c>.
	/// </summary>
	/// <remarks>This method removes all elements from the underlying buffer and sets the position to zero. After
	/// calling this method, the buffer will be empty, and any subsequent operations will start from the beginning of the
	/// buffer.</remarks>
	public void Clear()
	{
		_position = 0;
	}
	/// <summary>
	/// Determines whether the current buffer contains any of the specified characters.
	/// </summary>
	/// <param name="values">The set of characters to search for.</param>
	/// <returns><see langword="true"/> if any of the specified characters are found; otherwise, <see langword="false"/>.</returns>
	public readonly bool ContainsAny(SearchValues<char> values)
	{
		return _position > 0
			&& this.GetSpanUpToPosition().ContainsAny(values);
	}

	/// <summary>
	/// Copies the contents of this builder to a destination span.
	/// </summary>
	/// <param name="destination">The destination span to copy the contents to.</param>
	/// <returns>
	/// The number of <see cref="char"/> elements copied to the destination span.
	/// </returns>
	/// <exception cref="ArgumentException"/>
	public readonly int CopyTo(Span<char> destination)
	{
		_chars.UnsafeSlice(0, _position).CopyTo(destination);
		return _position;
	}

	/// <summary>
	/// Releases the resources used by the <see cref="SpanStringBuilder"/>, returning any rented buffers to the pool.
	/// </summary>
	/// <remarks>
	/// This is automatically called when invoking <see cref="Build"/>.
	/// </remarks>
	public void Dispose()
	{
		var (array, clearArray) = (_array, _clearOnDispose);
		this = default;

		Rent.Return(array, clearArray);
	}
	/// <summary>
	/// Determines whether the buffered content ends with the specified character sequence.
	/// </summary>
	/// <remarks>Returns <see langword="false"/> if the buffer is empty or if <paramref name="value"/> is longer
	/// than the current buffered content.</remarks>
	/// <param name="value">The character sequence to compare at the end of the buffered content.</param>
	/// <param name="comparisonType">The comparison rules to use when matching characters.</param>
	/// <returns><see langword="true"/> if the buffered content ends with <paramref name="value"/>; otherwise, <see
	/// langword="false"/>.</returns>
	public readonly bool EndsWith(ReadOnlySpan<char> value, StringComparison comparisonType = StringComparison.Ordinal)
	{
		return _position > 0
			&& _position >= value.Length
			&& this.GetSpanUpToPosition()
				   .EndsWith(value, comparisonType);
	}
	/// <summary>
	/// Returns a slice of the current buffer starting at the specified index and length.
	/// </summary>
	/// <param name="start">The starting index of the slice.</param>
	/// <param name="length">The length of the slice.</param>
	/// <exception cref="ArgumentOutOfRangeException"/>
	/// <returns>A <see cref="ReadOnlySpan{T}"/> representing the specified slice.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal readonly Span<char> GetSegment(int start, int length)
	{
		return _chars.Slice(start, length);
	}
	/// <summary>
	/// Returns a slice of the current buffer corresponding to the specified segment position.
	/// </summary>
	/// <param name="position">The position record describing the start index and length of the slice.</param>
	/// <returns>A <see cref="ReadOnlySpan{T}"/> representing the specified slice, or an empty span when
	/// <paramref name="position"/> is undefined.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="position"/> ends past
	/// <see cref="Capacity"/>.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal readonly ReadOnlySpan<char> GetSegment(SpanPosition position)
	{
		if (!position.IsDefined)
		{
			return [];
		}

		SpanPosition.ThrowIfOutOfRange(position, _chars.Length);
		return _chars.UnsafeSlice(position.Index, position.Length);
	}

	/// <summary>
	/// Finds the index of the specified character within the appended characters.
	/// </summary>
	/// <param name="value">The character to locate.</param>
	/// <returns>The zero-based index of the first occurrence of the character, or -1 if not found.</returns>
	[DebuggerStepThrough]
	public readonly int IndexOf(char value)
	{
		return this.GetSpanUpToPosition().IndexOf(value);
	}
	/// <summary>
	/// Finds the index of the specified character within the appended characters, starting from the specified index.
	/// </summary>
	/// <param name="value">The character to locate.</param>
	/// <param name="startIndex">The zero-based index at which to begin the search.</param>
	/// <returns>
	/// The zero-based index, counted from the start of the builder, of the first occurrence of the character at or after
	/// <paramref name="startIndex"/>, or -1 if the character is not found.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="startIndex"/> is less than 0 or greater than <see cref="Length"/>.
	/// </exception>
	[DebuggerStepThrough]
	public readonly int IndexOf(char value, int startIndex)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)startIndex, (uint)_position, nameof(startIndex));
		int index = _chars.UnsafeSlice(startIndex, _position - startIndex).IndexOf(value);
		return index < 0 ? index : index + startIndex;
	}
	/// <summary>
	/// Finds the index of the specified character within the appended characters, starting from the specified index and searching up to the specified count of characters.
	/// </summary>
	/// <param name="value">The character to locate.</param>
	/// <param name="startIndex">The zero-based index at which to begin the search.</param>
	/// <param name="count">The maximum number of characters to examine during the search.</param>
	/// <returns>
	/// The zero-based index, counted from the start of the builder, of the first occurrence of the character within the
	/// specified range, or -1 if the character is not found there.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="startIndex"/> is less than 0 or greater than <see cref="Length"/>, or when <paramref name="count"/> is less than 0 or exceeds the number of characters from <paramref name="startIndex"/> to <see cref="Length"/>.
	/// </exception>
	[DebuggerStepThrough]
	public readonly int IndexOf(char value, int startIndex, int count)
	{
		// Comparing as unsigned values rejects a negative argument along with one that's too large, and the subtraction
		// can't overflow once startIndex is known to lie within the appended characters.
		ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)startIndex, (uint)_position, nameof(startIndex));
		ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)count, (uint)(_position - startIndex), nameof(count));
		int index = _chars.UnsafeSlice(startIndex, count).IndexOf(value);
		return index < 0 ? index : index + startIndex;
	}

	/// <summary>
	/// Inserts a single character at the specified index in the buffer, shifting subsequent characters to the right.
	/// </summary>
	/// <param name="index">The zero-based index where the character will be inserted. An index equal to <see cref="Length"/>
	/// appends the character.</param>
	/// <param name="c">The character to insert.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="index"/> is less than 0 or greater than <see cref="Length"/>.
	/// </exception>
	/// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	[DebuggerStepThrough]
	public void Insert(int index, char c)
	{
		this.Insert(index, c, 1);
	}
	/// <summary>
	/// Inserts a character at the specified index in the buffer, shifting subsequent characters to the right.
	/// </summary>
	/// <param name="index">The zero-based index where the characters will be inserted. An index equal to
	/// <see cref="Length"/> appends them.</param>
	/// <param name="c">The character to insert.</param>
	/// <param name="count">The number of times to insert the character.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="index"/> is less than 0 or greater than <see cref="Length"/>, or when
	/// <paramref name="count"/> is negative.
	/// </exception>
	/// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	public void Insert(int index, char c, int count)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)_position, nameof(index));
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		if (count == 0)
		{
			return;
		}

		this.EnsureCapacity(new pint(count, true));
		int remaining = _position - index;

		var sliceToCopy = _chars.UnsafeSlice(index, remaining);
		var copyToSlice = _chars.UnsafeSlice(index + count);
		sliceToCopy.UnsafeCopy(copyToSlice);

		var sliceToFill = _chars.UnsafeSlice(index, count);
		if (c != default)
		{
			sliceToFill.Fill(c);
		}
		else
		{
			sliceToFill.Clear();
		}

		_position += count;
	}
	/// <summary>
	/// Inserts a span of characters at the specified index in the buffer, shifting subsequent characters to the right.
	/// </summary>
	/// <param name="index">The zero-based index where the characters will be inserted. An index equal to
	/// <see cref="Length"/> appends them.</param>
	/// <param name="value">The span of characters to insert. It can't be a span over the builder, even when the call
	/// doesn't grow the buffer, because the call moves the characters from <paramref name="index"/> on before it reads
	/// <paramref name="value"/>.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="index"/> is less than 0 or greater than <see cref="Length"/>.
	/// </exception>
	/// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	public void Insert(int index, scoped ReadOnlySpan<char> value)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)_position, nameof(index));

		if (value.IsEmpty)
		{
			return;
		}

		Debug.Assert(!value.Overlaps(_chars), "A span over the builder was passed to Insert, which moves the characters that the span covers before it reads them.");
		this.EnsureCapacity(new pint(value.Length, true));
		int remaining = _position - index;

		var sliceToCopy = _chars.UnsafeSlice(index, remaining);
		var copyToSlice = _chars.UnsafeSlice(index + value.Length);
		sliceToCopy.UnsafeCopy(copyToSlice); // Overlapping shift - relies on UnsafeCopy's memmove semantics.
		value.UnsafeCopy(_chars.UnsafeSlice(index));

		_position += value.Length;
	}

	/// <summary>
	/// Writes characters at the specified index and sets the position to the end of the written data.
	/// </summary>
	/// <remarks>Overwrites any existing data starting at the specified index. When <paramref name="value"/> is
	/// non-empty, sets the position to <paramref name="index"/> plus the length of <paramref name="value"/>, which may
	/// extend or reduce the current position, and ensures capacity only if the write extends beyond the current position.
	/// An empty <paramref name="value"/> leaves the builder unchanged. Passing an <paramref name="index"/> equal to the
	/// current <see cref="Length"/> appends at the end. This method is not thread-safe.</remarks>
	/// <param name="index">The zero-based index at which to begin writing. Must be between <c>0</c> and the current <see cref="Length"/>, inclusive.</param>
	/// <param name="value">The characters to write.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative or greater than the current <see cref="Length"/>.</exception>
	/// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	public void OverlapAppend(int index, scoped ReadOnlySpan<char> value)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)_position, nameof(index));

		if (!value.IsEmpty)
		{
			int newEnd = index + value.Length;               // correct end-of-data after the write
			int growth = newEnd - _position;                 // how much we extend past current end

			Debug.Assert(growth <= _chars.Length - _position || !value.Overlaps(_chars), "A span over the builder was passed to a call that grows its buffer.");
			if (growth > 0)
				this.EnsureCapacity(new pint(growth, true)); // EnsureCapacity(additionalBeyondPosition)

			value.UnsafeCopy(_chars.UnsafeSlice(index));
			_position = newEnd;
		}
	}
	/// <summary>
	/// Creates a overwriting slice from the builder that can be written into to.
	/// </summary>
	/// <param name="index">The zero-based index at which to begin writing. Must be between <c>0</c> and the current <see cref="Length"/>, inclusive.</param>
	/// <param name="length">The number of characters to write.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative or greater than the current <see cref="Length"/>.</exception>
	/// <inheritdoc cref="EnsureCapacity(pint)" path="/exception"/>
	public Span<char> OverlapAppendSpan(int index, int length)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(length);
		ThrowHelper.ThrowIfNegativeOrGreaterThan(index, (uint)_position);

		if (length == 0)
		{
			return [];
		}

		int newEnd = index + length;               // correct end-of-data after the slice
		int growth = newEnd - _position;           // how much we extend past current end

		if (growth > 0)
			this.EnsureCapacity(new pint(growth, true)); // EnsureCapacity(additionalBeyondPosition)

		Span<char> buffer = _chars.UnsafeSlice(index, length);
		_position = newEnd; // Set the position to the new end
		return buffer;
	}

	/// <summary>
	/// Removes the specified range of characters from the buffer.
	/// </summary>
	/// <param name="startIndex">The zero-based index at which to start removing characters.</param>
	/// <param name="length">The number of characters to remove.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="startIndex"/> is out of range for the current content
	/// or when <paramref name="length"/> exceeds the remaining characters starting at <paramref name="startIndex"/>.
	/// </exception>
	public void Remove(int startIndex, int length)
	{
		int pos = _position;
		// Bounds check without risk of addition overflow:
		//  - startIndex must be in [0, pos]
		//  - length must be in [0, pos - startIndex]
		if ((uint)startIndex > (uint)pos || (uint)length > (uint)(pos - startIndex))
		{
			throwArgumentOutOfRangeException(startIndex, length, pos);
		}

		// Fast no-op, once the range is known to be valid
		if (length == 0)
			return;

		// Move tail left over the removed segment (CopyTo handles overlap)
		// Only the "used" slice matters; no need to touch unused capacity past _position.
		Span<char> used = _chars.UnsafeSlice(0, pos);
		used.UnsafeSlice(startIndex + length).UnsafeCopy(used.UnsafeSlice(startIndex));

		_position = pos - length;

		[DoesNotReturn, StackTraceHidden, DebuggerStepThrough, MethodImpl(MethodImplOptions.NoInlining)]
		static void throwArgumentOutOfRangeException(int startIndex, int length, int pos)
		{
			throw new ArgumentOutOfRangeException(
				paramName: (uint)startIndex > (uint)pos ? nameof(startIndex) : nameof(length));
		}
	}

	/// <summary>
	/// Allocates a new <see cref="string"/> from the characters appended to the builder.
	/// </summary>
	/// <returns>
	/// The constructed <see cref="string"/> instance.
	/// </returns>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override readonly string ToString()
	{
		return _position > 0 ? new(this.GetSpanUpToPosition()) : string.Empty;
	}

	/// <summary>
	/// Moves the current position of the builder back to an index that excludes the specified character from the end of the buffer.
	/// </summary>
	/// <param name="trimChar">The character to remove from the end of the buffer.</param>
	public void TrimEnd(char trimChar)
	{
		if (_position > 0)
		{
			ref char cRef = ref _chars.Ref();

			while (_position > 0 && cRef.Offset(_position - 1) == trimChar)
			{
				_position--;
			}
		}
	}

	/// <summary>
	/// Replaces a temporary formatting buffer with a larger pooled one, returning any previous rental to the pool.
	/// </summary>
	/// <remarks>
	/// The previous contents are not carried over - every caller reformats the value from the start of the replacement
	/// buffer.
	/// </remarks>
	/// <param name="rented">
	/// The array currently backing the scratch buffer, or <see langword="null"/> while the buffer is still
	/// stack-allocated. Receives the newly rented array.
	/// </param>
	/// <param name="minimumCapacity">The minimum number of characters the replacement buffer must hold.</param>
	/// <returns>A <see cref="Span{T}"/> spanning the full length of the newly rented array.</returns>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Span<char> GrowScratch(ref char[]? rented, int minimumCapacity)
	{
		char[] array = Rent.Array<char>(minimumCapacity);
		Rent.Return(rented);
		rented = array;

		return array.AsSpan();
	}

	/// <summary>
	/// Ensures that the buffer has enough capacity to accommodate the specified number of additional characters.
	/// </summary>
	/// <param name="appendLength">The number of characters to accommodate.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="appendLength"/> is negative.</exception>
	/// <inheritdoc cref="Grow(pint)" path="/exception"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EnsureCapacity(int appendLength)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(appendLength);
		this.EnsureCapacity(new pint(appendLength, true));
	}
	/// <summary>
	/// Ensures that the buffer has enough capacity to accommodate the specified number of additional characters.
	/// </summary>
	/// <param name="appendLength">The number of characters to accommodate.</param>
	/// <inheritdoc cref="Grow(pint)" path="/exception"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EnsureCapacity(pint appendLength)
	{
		pint calculatedLength = pint.Clamp(unchecked((uint)_position + (uint)appendLength));
		if (calculatedLength > this.Capacity)
		{
			this.Grow(calculatedLength);
		}
	}

	/// <summary>
	/// Returns a span representing the current contents of the <see cref="SpanStringBuilder"/> up to the current position.
	/// </summary>
	/// <returns>A <see cref="Span{Char}"/> representing the current contents of the builder.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private readonly Span<char> GetSpanUpToPosition()
	{
		return _chars.UnsafeSlice(0, _position);
	}
	/// <summary>
	/// Increases the buffer's capacity to ensure it can accommodate at least the specified minimum number of elements.
	/// </summary>
	/// <remarks>The method typically doubles the current capacity, but may increase it further to meet the minimum
	/// requirement or to maintain efficient growth. Capacity is always rounded up to the next power of two, subject to
	/// platform array size limits. This method does not shrink the buffer.</remarks>
	/// <param name="minimumCapacity">The minimum required capacity for the buffer after growth. Must be greater than the current capacity.</param>
	/// <exception cref="BufferLimitException">Thrown when the requested minimum capacity exceeds the maximum allowable array size.</exception>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private void Grow(pint minimumCapacity)
	{
		// The maximum capacity of a .NET array on 32-bit and 64-bit platforms (2,147,483,591).
		const ulong MaxCapacity = 0X7FFFFFC7;

		ulong cap = (ulong)this.Capacity;

		// Base growth target: double current capacity (maintain power-of-2 growth).
		if (cap > MaxCapacity)
			throwTooBig(minimumCapacity, this.Capacity);

		ulong doubled = cap == 0 ? 1UL : (cap << 1);

		ulong min = minimumCapacity;
		ulong chosen;

		if (min >= doubled)
		{
			// Need at least the requested minimum: round it up to a power of 2.
			chosen = BitOperations.RoundUpToPowerOf2(min);
		}
		else
		{
			// 80% threshold within the (cap..doubled) window:
			// threshold = cap + ceil((doubled - cap) * 0.80)
			// With doubled == 2*cap, (doubled - cap) == cap.
			const ulong Numerator = 4;   // 0.80 = 4/5
			const ulong Denominator = 5;

			ulong gap = doubled - cap; // normally == cap

			if (gap > ulong.MaxValue / Numerator)
				throwTooBig(minimumCapacity, this.Capacity);

			ulong bump = (gap * Numerator + (Denominator - 1)) / Denominator; // ceil(gap*4/5)
			ulong threshold = cap + bump;

			// The 4x jump is headroom rather than a requirement - 'doubled' already covers 'min' on
			// this branch - so when the jump would run past the array limit, settle for the doubling
			// instead of failing a request that can still be served.
			chosen = min >= threshold && doubled <= MaxCapacity >> 1
				? doubled << 1 // jump to the next power-of-2 level (cap*4)
				: doubled;     // normal doubling (cap*2)
		}

		// The doubling branches carry through whatever capacity a caller-provided buffer had, which is
		// rarely a power of 2. Rounding here keeps the rented length - and the assertion below - honest;
		// the pool rounds the rental up to the same bucket regardless.
		chosen = BitOperations.RoundUpToPowerOf2(chosen);

		// RoundUpToPowerOf2 returns 0 on overflow; also enforce array limits.
		if (chosen == 0 || chosen > MaxCapacity)
			throwTooBig(minimumCapacity, this.Capacity);

		int newCapacity = (int)chosen;

		Debug.Assert(newCapacity > this.Capacity, "The new capacity is smaller or equal to the current capacity.");
		Debug.Assert((newCapacity & (newCapacity - 1)) == 0, "The new capacity is not a power of 2.");

		char[] newArray = Rent.Array<char>(newCapacity);
		Span<char> newSpan = newArray.AsUnsafeSpan(0);
		_chars.UnsafeCopy(newSpan);

		(char[]? oldArray, _array) = (_array, newArray);
		_chars = newSpan;

		Rent.Return(oldArray, _clearOnDispose);

		[DoesNotReturn, StackTraceHidden, DebuggerStepThrough, MethodImpl(MethodImplOptions.NoInlining)]
		static void throwTooBig(int requestedLength, int previousLength)
		{
			throw new BufferLimitException((long)MaxCapacity, previousLength, requestedLength);
		}
	}
}