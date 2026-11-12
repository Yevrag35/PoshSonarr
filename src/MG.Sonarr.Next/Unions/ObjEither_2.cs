using System.ComponentModel;

namespace MG.Sonarr.Next.Unions;

/// <summary>
/// Represents a discriminated union that holds a reference to either an object of type <typeparamref name="T1"/> or an object of type <typeparamref name="T2"/>.
/// </summary>
/// <remarks>
/// <para>Use <see cref="ObjEither{T1, T2}"/> to encapsulate a value that may be one of two distinct reference types,
/// enabling type-safe access and pattern matching. The struct provides properties and methods to determine which type
/// is currently held and to retrieve the value accordingly. Implicit conversion operators allow for convenient
/// construction from either type.</para>
/// <para>An instance that holds a type holds a non-null value of it: the constructors and the implicit conversions
/// throw <see cref="ArgumentNullException"/> for <see langword="null"/>. Only the <see langword="default"/> instance
/// holds no value, and it holds neither type.</para>
/// </remarks>
/// <typeparam name="T1">The first reference type that can be stored in the union.</typeparam>
/// <typeparam name="T2">The second reference type that can be stored in the union.</typeparam>
[Union, StructLayout(LayoutKind.Auto), DebuggerDisplay(@"\{Index = {_index}, Value = {_value}\}"), DebuggerStepThrough]
public readonly partial struct ObjEither<T1, T2> : IUnion where T1 : class where T2 : class // Size = 16
{
	private readonly object? _value;
	private readonly byte _index;

	/// <summary>
	/// Get a value that indicates whether the union currently holds a <typeparamref name="T1"/> value.
	/// </summary>
	/// <value><see langword="true"/> if the value is of type <typeparamref name="T1"/>; otherwise, <see langword="false"/>.</value>
	[MemberNotNullWhen(true, nameof(AsT1), nameof(_value), nameof(Value))]
	public bool IsT1 => _index == 1;

	/// <summary>
	/// Get a value that indicates whether the union currently holds a <typeparamref name="T2"/> value.
	/// </summary>
	/// <value><see langword="true"/> if the value is of type <typeparamref name="T2"/>; otherwise, <see langword="false"/>.</value>
	[MemberNotNullWhen(true, nameof(AsT2), nameof(Value), nameof(_value))]
	public bool IsT2 => _index == 2;

	/// <summary>
	/// Get the discriminant index for the current value.
	/// </summary>
	/// <remarks>
	/// <para>The value is 0 when this instance is <see langword="default"/>, 1 when it holds a <typeparamref name="T1"/>, and 2 when it holds a <typeparamref name="T2"/>.</para>
	/// </remarks>
	/// <value>The discriminant index for the current value.</value>
	public byte Index => _index;
	/// <summary>
	/// Get a value that indicates whether this instance holds no value.
	/// </summary>
	/// <remarks>
	/// <para>This state occurs when the instance is the <see langword="default"/> value.</para>
	/// </remarks>
	/// <value><see langword="true"/> if this instance holds neither a <typeparamref name="T1"/> nor a <typeparamref name="T2"/>; otherwise, <see langword="false"/>.</value>
	[MemberNotNullWhen(false, nameof(_value), nameof(Value))]
	public bool IsNeither => _index == 0;

	/// <summary>
	/// Get a value that indicates whether this instance is in its default state, holding neither a <typeparamref name="T1"/> nor a <typeparamref name="T2"/>.
	/// </summary>
	[DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
	public bool IsDefault => this.IsNeither;

	/// <summary>
	/// Get the value as <typeparamref name="T1"/>.
	/// </summary>
	/// <remarks>
	/// <para>This property returns <see langword="null"/> when <see cref="IsT1"/> is <see langword="false"/>.</para>
	/// </remarks>
	/// <value>The stored value cast to <typeparamref name="T1"/>, or <see langword="null"/>.</value>
	public T1? AsT1 => _value as T1;

	/// <summary>
	/// Get the value as <typeparamref name="T2"/>.
	/// </summary>
	/// <remarks>
	/// <para>This property returns <see langword="null"/> when <see cref="IsT2"/> is <see langword="false"/>.</para>
	/// </remarks>
	/// <value>The stored value cast to <typeparamref name="T2"/>, or <see langword="null"/>.</value>
	public T2? AsT2 => _value as T2;

	/// <summary>
	/// Gets a value that indicates whether this instance holds a value, which only the <see langword="default"/>
	/// instance doesn't.
	/// </summary>
	/// <value><see langword="true"/> if this instance holds a value; otherwise, <see langword="false"/>.</value>
	[MemberNotNullWhen(true, nameof(Value), nameof(_value))]
	public bool HasValue => _index != 0;
	/// <summary>
	/// Gets the underlying value stored in the union, which can be either of type <typeparamref name="T1"/> or <typeparamref name="T2"/>.
	/// </summary>
	/// <value>The underlying stored value, or <see langword="null"/> if this instance is <see langword="default"/>.</value>
	public object? Value => _value;

	/// <summary>
	/// Initializes a new <see cref="ObjEither{T1, T2}"/> instance that holds a <typeparamref name="T1"/> value.
	/// </summary>
	/// <param name="value">The value to store.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public ObjEither(T1 value)
	{
		ArgumentNullException.ThrowIfNull(value);
		_value = value;
		_index = 1;
	}

	/// <summary>
	/// Initializes a new <see cref="ObjEither{T1, T2}"/> instance that holds a <typeparamref name="T2"/> value.
	/// </summary>
	/// <param name="value">The value to store.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public ObjEither(T2 value)
	{
		ArgumentNullException.ThrowIfNull(value);
		_value = value;
		_index = 2;
	}

	/// <summary>
	/// Try to get the stored value as <typeparamref name="T1"/>.
	/// </summary>
	/// <remarks>
	/// <para>When this instance holds a <typeparamref name="T2"/>, <paramref name="other"/> is set and the method returns <see langword="false"/>.</para>
	/// </remarks>
	/// <param name="value">When this method returns <see langword="true"/>, receives the stored <typeparamref name="T1"/> value; otherwise, <see langword="null"/>.</param>
	/// <param name="other">When this method returns <see langword="false"/>, receives the stored <typeparamref name="T2"/> value; otherwise, <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if the stored value is a <typeparamref name="T1"/>; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="InvalidOperationException">Thrown when this instance is default.</exception>
	public bool TryGetT1([NotNullWhen(true)] out T1? value, [NotNullWhen(false)] out T2? other)
	{
		bool result = false;
		unsafe
		{
			// SAFETY: _index is 1 only when _value holds a T1 and 2 only when it holds a T2, as the constructors set them.
			switch (_index)
			{
				case 1:
					value = Unsafe.As<T1>(_value!);
					other = null;
					result = true;
					break;

				case 2:
					value = null;
					other = Unsafe.As<T2>(_value!);
					break;

				default:
					value = null;
					other = null;
					return Exceptions.ThrowInvalidIndexException();
			}
		}

		return result;
	}
	/// <summary>
	/// Try to get the stored value as <typeparamref name="T2"/>.
	/// </summary>
	/// <remarks>
	/// <para>When this instance holds a <typeparamref name="T1"/>, <paramref name="other"/> is set and the method returns <see langword="false"/>.</para>
	/// </remarks>
	/// <param name="value">When this method returns <see langword="true"/>, receives the stored <typeparamref name="T2"/> value; otherwise, <see langword="null"/>.</param>
	/// <param name="other">When this method returns <see langword="false"/>, receives the stored <typeparamref name="T1"/> value; otherwise, <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if the stored value is a <typeparamref name="T2"/>; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="InvalidOperationException">Thrown when this instance is default.</exception>
	public bool TryGetT2([NotNullWhen(true)] out T2? value, [NotNullWhen(false)] out T1? other)
	{
		bool result = false;
		unsafe
		{
			// SAFETY: _index is 1 only when _value holds a T1 and 2 only when it holds a T2, as the constructors set them.
			switch (_index)
			{
				case 1:
					other = Unsafe.As<T1>(_value)!;
					value = null;
					break;

				case 2:
					value = Unsafe.As<T2>(_value)!;
					other = null;
					result = true;
					break;

				default:
					value = null;
					other = null;
					return Exceptions.ThrowInvalidIndexException();
			}
		}

		return result;
	}

	/// <summary>
	/// Create a union instance from a <typeparamref name="T1"/> value.
	/// </summary>
	/// <param name="value">The value to store.</param>
	/// <returns>A union holding <paramref name="value"/> as a <typeparamref name="T1"/> value.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static implicit operator ObjEither<T1, T2>(T1 value) => new(value);
	/// <summary>
	/// Create a union instance from a <typeparamref name="T2"/> value.
	/// </summary>
	/// <param name="value">The value to store.</param>
	/// <returns>A union holding <paramref name="value"/> as a <typeparamref name="T2"/> value.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static implicit operator ObjEither<T1, T2>(T2 value) => new(value);
}

file static class Exceptions
{
	/// <summary>
	/// Throws the exception for an <see cref="ObjEither{T1, T2}"/> whose index names neither of its types.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The method takes no type parameters and no arguments, so shared generic code that inlines
	/// <see cref="ObjEither{T1, T2}.TryGetT1"/> or <see cref="ObjEither{T1, T2}.TryGetT2"/> calls it without a lookup in
	/// its generic dictionary, and none of the caller's locals has its address taken. The callers set their
	/// <see langword="out"/> values before the call for that reason.
	/// </para>
	/// <para>
	/// It isn't marked <see cref="MethodImplOptions.NoInlining"/>, so the JIT reads its IL, finds that it never returns,
	/// and moves the call off the caller's main path.
	/// </para>
	/// </remarks>
	/// <returns>This method doesn't return; it always throws.</returns>
	/// <exception cref="InvalidOperationException">Thrown when this method is called.</exception>
	[DoesNotReturn, StackTraceHidden, DebuggerStepThrough]
	internal static bool ThrowInvalidIndexException()
	{
		throw new InvalidOperationException($"{nameof(ObjEither<,>)} must have a valid index");
	}
}