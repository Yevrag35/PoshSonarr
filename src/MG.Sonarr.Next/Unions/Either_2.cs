namespace MG.Sonarr.Next.Unions;

/// <summary>
/// Represents a union of two possible types.
/// </summary>
/// <remarks>
/// <para>
/// Each type has a field of its own beside the index of the type that the union holds, so a value type is stored in
/// place and never boxed.
/// </para>
/// <para>
/// A <see langword="default"/> instance holds neither type: its <see cref="Index"/> is 0,
/// <see cref="IsDefaultOrEmpty"/> is <see langword="true"/>, and <see cref="Match{TOutput, TState}"/>,
/// <see cref="TryGetT1"/>, and <see cref="TryGetT2"/> throw <see cref="EmptyStructException"/> for it.
/// </para>
/// </remarks>
/// <typeparam name="T1">The first type.</typeparam>
/// <typeparam name="T2">The second type.</typeparam>
[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay("{GetDebugString(),nq}")]
public readonly partial struct Either<T1, T2>
{
	private readonly T1? _first;
	private readonly T2? _second;

	private readonly byte _index;

	/// <summary>
	/// Gets the value of the first type if present.
	/// </summary>
	/// <value>The first value, or <see langword="default"/> when the instance doesn't hold a <typeparamref name="T1"/>.</value>
	public readonly T1? AsT1 => _first;

	/// <summary>
	/// Gets the value of the second type if present.
	/// </summary>
	/// <value>The second value, or <see langword="default"/> when the instance doesn't hold a <typeparamref name="T2"/>.</value>
	public readonly T2? AsT2 => _second;

	/// <summary>
	/// Gets the index of the type that the instance holds.
	/// </summary>
	/// <value>
	/// 1 when the instance holds a <typeparamref name="T1"/>, 2 when it holds a <typeparamref name="T2"/>, and 0 for a
	/// <see langword="default"/> instance.
	/// </value>
	public byte Index => _index;

	/// <summary>
	/// Gets a value that indicates whether the instance is a <see langword="default"/> instance, which holds neither type.
	/// </summary>
	/// <value><see langword="true"/> when the instance holds neither type; otherwise, <see langword="false"/>.</value>
	public readonly bool IsDefaultOrEmpty => _index == 0;

	/// <summary>
	/// Gets a value that indicates whether the instance holds the first type.
	/// </summary>
	/// <value><see langword="true"/> when the instance stores a <typeparamref name="T1"/> value; otherwise, <see langword="false"/>.</value>
	[MemberNotNullWhen(true, nameof(_first), nameof(AsT1))]
	public readonly bool IsT1 => _index == 1;

	/// <summary>
	/// Gets a value that indicates whether the instance holds the second type.
	/// </summary>
	/// <value><see langword="true"/> when the instance stores a <typeparamref name="T2"/> value; otherwise, <see langword="false"/>.</value>
	[MemberNotNullWhen(true, nameof(_second), nameof(AsT2))]
	public readonly bool IsT2 => _index == 2;

	/// <summary>
	/// Initializes a new <see cref="Either{T1, T2}"/> instance that holds a value of the first type.
	/// </summary>
	/// <param name="first">The value of the first type.</param>
	private Either(T1 first)
	{
		_first = first;
		_second = default;
		_index = 1;
	}

	/// <summary>
	/// Initializes a new <see cref="Either{T1, T2}"/> instance that holds a value of the second type.
	/// </summary>
	/// <param name="second">The value of the second type.</param>
	private Either(T2 second)
	{
		_first = default;
		_second = second;
		_index = 2;
	}

	/// <summary>
	/// Determines whether this instance and another instance store the same union arm.
	/// </summary>
	/// <remarks>
	/// Two <see langword="default"/> instances store no arm, so they don't store the same one.
	/// </remarks>
	/// <param name="other">The instance to compare.</param>
	/// <returns><see langword="true"/> when both instances store the same arm; otherwise, <see langword="false"/>.</returns>
	public readonly bool IsSameType(in Either<T1, T2> other)
	{
		return this.IsSameType(in other, ignoreDefaultOrEmpty: false);
	}

	/// <summary>
	/// Determines whether this instance and another instance store the same union arm, optionally counting two
	/// <see langword="default"/> instances as the same.
	/// </summary>
	/// <param name="other">The instance to compare.</param>
	/// <param name="ignoreDefaultOrEmpty">
	/// <see langword="true"/> to count two <see langword="default"/> instances as storing the same arm; otherwise,
	/// <see langword="false"/>.
	/// </param>
	/// <returns><see langword="true"/> when both instances satisfy the same-type comparison; otherwise, <see langword="false"/>.</returns>
	public readonly bool IsSameType(in Either<T1, T2> other, bool ignoreDefaultOrEmpty)
	{
		return (other._index > 0 || ignoreDefaultOrEmpty) && _index == other._index;
	}

	/// <summary>
	/// Matches the current instance to one of the provided functions based on its type and returns the result, with state.
	/// </summary>
	/// <typeparam name="TOutput">The output type.</typeparam>
	/// <typeparam name="TState">The state type, which can be a by-ref-like type.</typeparam>
	/// <param name="state">The state to pass to the functions.</param>
	/// <param name="f1">The function to execute if the instance is of the first type.</param>
	/// <param name="f2">The function to execute if the instance is of the second type.</param>
	/// <returns>The result of the executed function.</returns>
	/// <exception cref="EmptyStructException">Thrown when the instance is a default instance, which holds neither type.</exception>
	public readonly TOutput Match<TOutput, TState>(
		TState state,
		Func<T1, TState, TOutput> f1,
		Func<T2, TState, TOutput> f2)
		where TState : allows ref struct
	{
		switch (_index)
		{
			case 1:
				return f1(_first!, state);

			case 2:
				return f2(_second!, state);

			default:
				EitherThrowHelper.ThrowEmpty();
				return default!;
		}
	}

	/// <summary>
	/// Tries to get the value of the first type.
	/// </summary>
	/// <param name="t1">The value of the first type if present.</param>
	/// <param name="t2">The value of the second type if present.</param>
	/// <returns><see langword="true"/> if the instance is of the first type; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="EmptyStructException">Thrown when the instance is a default instance, which holds neither type.</exception>
	public readonly bool TryGetT1([NotNullWhen(true)] out T1? t1, [NotNullWhen(false)] out T2? t2)
	{
		if (this.IsDefaultOrEmpty)
		{
			EitherThrowHelper.ThrowEmpty();
		}

		t1 = _first;
		t2 = _second;
		return this.IsT1;
	}
	/// <summary>
	/// Tries to get the value of the second type.
	/// </summary>
	/// <param name="t2">The value of the second type if present.</param>
	/// <param name="t1">The value of the first type if present.</param>
	/// <returns><see langword="true"/> if the instance is of the second type; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="EmptyStructException">Thrown when the instance is a default instance, which holds neither type.</exception>
	public readonly bool TryGetT2([NotNullWhen(true)] out T2? t2, [NotNullWhen(false)] out T1? t1)
	{
		if (this.IsDefaultOrEmpty)
		{
			EitherThrowHelper.ThrowEmpty();
		}

		t1 = _first;
		t2 = _second;
		return this.IsT2;
	}

	/// <summary>
	/// Creates a new <see cref="Either{T1, T2}"/> instance from a first-type value.
	/// </summary>
	/// <param name="value">The first-type value to wrap.</param>
	/// <returns>An either instance that stores a <typeparamref name="T1"/> value.</returns>
	public static Either<T1, T2> FromT1(T1 value) => new(value);

	/// <summary>
	/// Creates a new <see cref="Either{T1, T2}"/> instance from a second-type value.
	/// </summary>
	/// <param name="value">The second-type value to wrap.</param>
	/// <returns>An either instance that stores a <typeparamref name="T2"/> value.</returns>
	public static Either<T1, T2> FromT2(T2 value) => new(value);

	/// <summary>
	/// Gets the text that the debugger shows for the instance.
	/// </summary>
	/// <returns>
	/// The type that the instance holds and its value, such as <c>T1: 5</c>, or <c>Default</c> for a
	/// <see langword="default"/> instance.
	/// </returns>
	private string GetDebugString()
	{
		return _index switch
		{
			1 => $"T1: {_first}",
			2 => $"T2: {_second}",
			_ => "Default",
		};
	}

	/// <summary>
	/// Implicitly converts a value of the first type to an Either instance.
	/// </summary>
	/// <param name="first">The value of the first type.</param>
	/// <returns>An either instance that stores <paramref name="first"/> as a <typeparamref name="T1"/> value.</returns>
	public static implicit operator Either<T1, T2>(T1 first) => new(first);

	/// <summary>
	/// Implicitly converts a value of the second type to an Either instance.
	/// </summary>
	/// <param name="second">The value of the second type.</param>
	/// <returns>An either instance that stores <paramref name="second"/> as a <typeparamref name="T2"/> value.</returns>
	public static implicit operator Either<T1, T2>(T2 second) => new(second);
}

/// <summary>
/// Provides the throw helper of <see cref="Either{T1, T2}"/> that its shared generic code calls without a lookup of
/// its type arguments.
/// </summary>
file static class EitherThrowHelper
{
	/// <summary>
	/// Throws an <see cref="EmptyStructException"/> for a default <see cref="Either{T1, T2}"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The method takes no type parameters and no arguments, and the exception names the generic type definition,
	/// because naming a constructed type would take the type arguments, which a caller's shared code would look up in
	/// its generic dictionary on every call, ahead of the check that leads here, only to have them ready for the throw.
	/// </para>
	/// <para>
	/// It isn't marked <see cref="MethodImplOptions.NoInlining"/>, so the JIT reads its IL, finds that it never returns,
	/// and moves the call off the caller's main path.
	/// </para>
	/// </remarks>
	/// <exception cref="EmptyStructException">Thrown when this method is called.</exception>
	[DoesNotReturn, StackTraceHidden, DebuggerStepThrough]
	internal static void ThrowEmpty()
	{
		throw new EmptyStructException(nameof(Either<,>), typeof(Either<,>), innerException: null);
	}
}
