using System.ComponentModel;

namespace MG.Sonarr.Next.Unions;

/// <summary>
/// Represents an action that receives a value and a parameter span.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="item">The value argument.</param>
/// <param name="parameters">The parameter span.</param>
public delegate void RefAction<T>(T item, ReadOnlySpan<object?> parameters);

/// <summary>
/// Represents a union of two possible types.
/// </summary>
/// <typeparam name="T1">The first type.</typeparam>
/// <typeparam name="T2">The second type.</typeparam>
[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay("{GetDebugString(),nq}")]
public readonly partial struct Either<T1, T2>
{
	private readonly bool _isNotDefault;
	private readonly T1? _first;
	private readonly T2? _second;

	private readonly byte _index;

	/// <summary>
	/// Gets the value of the first type if present.
	/// </summary>
	/// <value>The first value.</value>
	public readonly T1? AsT1 => _first;

	/// <summary>
	/// Gets the value of the second type if present.
	/// </summary>
	/// <value>The second value.</value>
	public readonly T2? AsT2 => _second;

	/// <summary>
	/// Gets the index indicating the current type.
	/// </summary>
	/// <value>The discriminator index.</value>
	public byte Index => _index;

	/// <summary>
	/// Gets a value indicating whether the instance is default or empty.
	/// </summary>
	/// <value><see langword="true"/> when the instance is default-initialized; otherwise, <see langword="false"/>.</value>
	public readonly bool IsDefaultOrEmpty => !_isNotDefault;

	/// <summary>
	/// Gets a value indicating whether the instance is of the first type.
	/// </summary>
	/// <value><see langword="true"/> when the instance stores a <typeparamref name="T1"/> value; otherwise, <see langword="false"/>.</value>
	[MemberNotNullWhen(true, nameof(_first), nameof(AsT1))]
	public readonly bool IsT1 => _index == 1;

	/// <summary>
	/// Gets a value indicating whether the instance is of the second type.
	/// </summary>
	/// <value><see langword="true"/> when the instance stores a <typeparamref name="T2"/> value; otherwise, <see langword="false"/>.</value>
	[MemberNotNullWhen(true, nameof(_second), nameof(AsT2))]
	public readonly bool IsT2 => _index == 2;

	/// <summary>
	/// Initializes a new instance of the <see cref="Either{T1, T2}"/> struct with the specified values and index.
	/// </summary>
	/// <param name="first">The value of the first type.</param>
	/// <param name="second">The value of the second type.</param>
	/// <param name="index">The index indicating the current type.</param>
	internal Either(T1? first, T2? second, byte index)
	{
		_isNotDefault = true;
		_first = first;
		_second = second;
		_index = index;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Either{T1, T2}"/> struct with the first type.
	/// </summary>
	/// <param name="first">The value of the first type.</param>
	private Either(T1 first)
	{
		_isNotDefault = true;
		_first = first;
		_second = default;
		_index = 1;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Either{T1, T2}"/> struct with the second type.
	/// </summary>
	/// <param name="second">The value of the second type.</param>
	private Either(T2 second)
	{
		_isNotDefault = true;
		_first = default;
		_second = second;
		_index = 2;
	}

	/// <summary>
	/// Determines whether this instance and another instance store the same union arm.
	/// </summary>
	/// <param name="other">The instance to compare.</param>
	/// <returns><see langword="true"/> when both instances store the same arm; otherwise, <see langword="false"/>.</returns>
	public readonly bool IsSameType(in Either<T1, T2> other)
	{
		return this.IsSameType(in other, ignoreDefaultOrEmpty: false);
	}

	/// <summary>
	/// Determines whether this instance and another instance store the same union arm.
	/// </summary>
	/// <param name="other">The instance to compare.</param>
	/// <param name="ignoreDefaultOrEmpty">A value that indicates whether default instances are treated as matching by index.</param>
	/// <returns><see langword="true"/> when both instances satisfy the same-type comparison; otherwise, <see langword="false"/>.</returns>
	public readonly bool IsSameType(in Either<T1, T2> other, bool ignoreDefaultOrEmpty)
	{
		return (other._index > 0 || ignoreDefaultOrEmpty) && _index == other._index;
	}

	/// <summary>
	/// Determines whether this instance and another instance store different union arms.
	/// </summary>
	/// <param name="other">The instance to compare.</param>
	/// <returns><see langword="true"/> when the instances store different arms; otherwise, <see langword="false"/>.</returns>
	public readonly bool IsNotSameType(in Either<T1, T2> other)
	{
		return _index != other._index;
	}

	/// <summary>
	/// Matches the current instance to one of the provided actions based on its type.
	/// </summary>
	/// <param name="f1">The action to execute if the instance is of the first type.</param>
	/// <param name="f2">The action to execute if the instance is of the second type.</param>
	public readonly void Match(
		Action<T1> f1,
		Action<T2> f2)
	{
		switch (_index)
		{
			case 0:
				goto default;

			case 1:
				f1(_first!);
				break;

			case 2:
				f2(_second!);
				break;

			default:
				throw new EmptyStructException(nameof(Either<,>), this.GetType(), innerException: null);
		}
	}
	/// <summary>
	/// Matches the current instance to one of the provided actions based on its type, with state.
	/// </summary>
	/// <remarks>
	/// This method exists when the state and return object types are simply <see cref="object"/> to reduce the 
	/// overhead of creating a new delegate type for each call.
	/// </remarks>
	/// <param name="state">The state to pass to the actions.</param>
	/// <param name="f1">The action to execute if the instance is of the first type.</param>
	/// <param name="f2">The action to execute if the instance is of the second type.</param>
	/// <returns>The result of the executed function.</returns>
	/// <exception cref="EmptyStructException">Thrown when the instance holds neither type, as a default-initialized one doesn't.</exception>
	public readonly object? MatchObj(
		object? state,
		Func<T1, object?, object?> f1,
		Func<T2, object?, object?> f2)
	{
		return _index switch
		{
			1 =>f1(_first!, state),
			2 =>f2(_second!, state),
			0 or _ =>throw new EmptyStructException(nameof(Either<,>), this.GetType(), innerException: null),
		};
	}
	/// <summary>
	/// Matches the current instance to one of the provided actions based on its type, with state.
	/// </summary>
	/// <typeparam name="TState">The state type.</typeparam>
	/// <param name="state">The state to pass to the actions.</param>
	/// <param name="f1">The action to execute if the instance is of the first type.</param>
	/// <param name="f2">The action to execute if the instance is of the second type.</param>
	/// <exception cref="EmptyStructException"></exception>
	public readonly void Match<TState>(
		TState state,
		Action<T1, TState> f1,
		Action<T2, TState> f2)
#if NET9_0_OR_GREATER
			where TState : allows ref struct
#endif
	{
		switch (_index)
		{
			case 0:
				goto default;

			case 1:
				f1(_first!, state);
				break;

			case 2:
				f2(_second!, state);
				break;

			default:
				throw new EmptyStructException(nameof(Either<,>), this.GetType(), innerException: null);
		}
	}
	/// <summary>
	/// Matches the current instance to one of the provided functions based on its type and returns the result.
	/// </summary>
	/// <typeparam name="TOutput">The output type.</typeparam>
	/// <param name="f1">The function to execute if the instance is of the first type.</param>
	/// <param name="f2">The function to execute if the instance is of the second type.</param>
	/// <returns>The result of the executed function.</returns>
	/// <exception cref="EmptyStructException">Thrown when the instance is default-initialized.</exception>
	public readonly TOutput Match<TOutput>(
		Func<T1, TOutput> f1,
		Func<T2, TOutput> f2)
#if NET9_0_OR_GREATER
			where TOutput : allows ref struct
#endif
	{
		return _index switch
		{
			1 =>f1(_first!),
			2 =>f2(_second!),
			0 or _ =>throw new EmptyStructException(nameof(Either<,>), this.GetType(), innerException: null),
		};
	}
	/// <summary>
	/// Matches the current instance to one of the provided functions based on its type and returns the result, with state.
	/// </summary>
	/// <typeparam name="TOutput">The output type.</typeparam>
	/// <typeparam name="TState">The state type.</typeparam>
	/// <param name="state">The state to pass to the functions.</param>
	/// <param name="f1">The function to execute if the instance is of the first type.</param>
	/// <param name="f2">The function to execute if the instance is of the second type.</param>
	/// <returns>The result of the executed function.</returns>
	/// <exception cref="EmptyStructException">Thrown when the instance is default-initialized.</exception>
	public readonly TOutput Match<TOutput, TState>(
		TState state,
		Func<T1, TState, TOutput> f1,
		Func<T2, TState, TOutput> f2)
#if NET9_0_OR_GREATER
			where TState : allows ref struct
#endif
	{
		return _index switch
		{
			1 =>f1(_first!, state),
			2 =>f2(_second!, state),
			0 or _ =>throw new EmptyStructException(nameof(Either<,>), this.GetType(), innerException: null),
		};
	}

	/// <summary>
	/// Matches the current instance to one of the provided function pointers based on its type and returns the result.
	/// </summary>
	/// <remarks>
	/// When the instance is default-initialized, the <see cref="EmptyStructException.StructType"/> of the exception
	/// is the generic type definition, <c>Either&lt;,&gt;</c>, and not the constructed type that the other overloads
	/// report.
	/// </remarks>
	/// <typeparam name="TOutput">The output type.</typeparam>
	/// <typeparam name="TState">The state type.</typeparam>
	/// <param name="state">The state to pass to the function pointer.</param>
	/// <param name="f1">The function pointer to execute if the instance is of the first type.</param>
	/// <param name="f2">The function pointer to execute if the instance is of the second type.</param>
	/// <returns>The result of the executed function pointer.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="f1"/> or <paramref name="f2"/> is null.</exception>
	/// <exception cref="EmptyStructException">Thrown when the instance is default-initialized.</exception>
	public unsafe TOutput Match<TOutput, TState>(
		TState state,
		delegate*<T1, TState, TOutput> f1,
		delegate*<T2, TState, TOutput> f2)
		where TState : allows ref struct
	{
		// A call through a null pointer ends the process with an access violation that no catch sees.
		if (f1 == null || f2 == null)
		{
			EitherThrowHelper.ThrowArgumentNull(f1 == null ? nameof(f1) : nameof(f2));
		}

		// The throw helpers aren't generic and name no constructed type, so shared code never looks up the type
		// arguments. A lookup would keep the generic context alive, at the cost of a stack store on every call.
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
	/// Matches the current instance to one of the provided actions based on its type, with state.
	/// </summary>
	/// <remarks>
	/// This method exists when the state type is simply <see cref="object"/> to reduce the 
	/// overhead of creating a new delegate type for each call.
	/// </remarks>
	/// <param name="state">The state to pass to the actions.</param>
	/// <param name="f1">The action to execute if the instance is of the first type.</param>
	/// <param name="f2">The action to execute if the instance is of the second type.</param>
	/// <exception cref="EmptyStructException"></exception>
	public readonly void MatchObj(
		object? state,
		Action<T1, object?> f1,
		Action<T2, object?> f2)
	{
		switch (_index)
		{
			case 0:
				goto default;

			case 1:
				f1(_first!, state);
				break;

			case 2:
				f2(_second!, state);
				break;

			default:
				throw new EmptyStructException(nameof(Either<,>), this.GetType(), innerException: null);
		}
	}
	/// <summary>
	/// Matches the current instance to one of the provided actions based on its type, with state.
	/// </summary>
	/// <remarks>
	/// This method exists when the state type is simply <see cref="object"/> to reduce the 
	/// overhead of creating a new delegate type for each call.
	/// </remarks>
	/// <param name="state">The state to pass to the actions.</param>
	/// <param name="f1">The action to execute if the instance is of the first type.</param>
	/// <param name="f2">The action to execute if the instance is of the second type.</param>
	/// <exception cref="EmptyStructException"></exception>
	public readonly void MatchObj(
		RefAction<T1> f1,
		RefAction<T2> f2,
		params ReadOnlySpan<object?> state)
	{
		switch (_index)
		{
			case 0:
				goto default;

			case 1:
				f1(_first!, state);
				break;

			case 2:
				f2(_second!, state);
				break;

			default:
				throw new EmptyStructException(nameof(Either<,>), this.GetType(), innerException: null);
		}
	}

	/// <summary>
	/// Tries to get the value of the first type.
	/// </summary>
	/// <param name="t1">The value of the first type if present.</param>
	/// <param name="t2">The value of the second type if present.</param>
	/// <returns><see langword="true"/> if the instance is of the first type, otherwise <see langword="false"/>.</returns>
	/// <exception cref="EmptyStructException"></exception>
	public readonly bool TryGetT1([NotNullWhen(true)] out T1? t1, [NotNullWhen(false)] out T2? t2)
	{
		EmptyStructException.ThrowIf(this.IsDefaultOrEmpty, this, nameof(Either<,>));
		t1 = _first;
		t2 = _second;
		return this.IsT1;
	}
	/// <summary>
	/// Tries to get the value of the second type.
	/// </summary>
	/// <param name="t2">The value of the second type if present.</param>
	/// <param name="t1">The value of the first type if present.</param>
	/// <returns><see langword="true"/> if the instance is of the second type, otherwise <see langword="false"/>.</returns>
	/// <exception cref="EmptyStructException"></exception>
	public readonly bool TryGetT2([NotNullWhen(true)] out T2? t2, [NotNullWhen(false)] out T1? t1)
	{
		EmptyStructException.ThrowIf(this.IsDefaultOrEmpty, this, nameof(Either<,>));
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

#if DEBUG
	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("Don't use this method - only for debugging display.")]
	private string GetDebugString()
	{
		return _index switch
		{
			1 =>$"T1: {_first}",
			2 =>$"T2: {_second}",
			0 or _ =>"Invalid",
		};
	}
#endif

	/// <summary>
	/// Implicitly converts a value of the first type to an Either instance.
	/// </summary>
	/// <param name="first">The value of the first type.</param>
	public static implicit operator Either<T1, T2>(T1 first) => new(first);

	/// <summary>
	/// Implicitly converts a value of the second type to an Either instance.
	/// </summary>
	/// <param name="second">The value of the second type.</param>
	public static implicit operator Either<T1, T2>(T2 second) => new(second);
}

/// <summary>
/// Provides the throw helpers of <see cref="Either{T1, T2}"/> that its shared generic code calls without a lookup of
/// its type arguments.
/// </summary>
file static class EitherThrowHelper
{
	/// <summary>
	/// Throws an <see cref="ArgumentNullException"/> for the specified parameter.
	/// </summary>
	/// <param name="paramName">The name of the null parameter.</param>
	/// <exception cref="ArgumentNullException">Thrown when this method is called.</exception>
	[DoesNotReturn, StackTraceHidden]
	internal static void ThrowArgumentNull(string paramName)
	{
		throw new ArgumentNullException(paramName);
	}

	/// <summary>
	/// Throws an <see cref="EmptyStructException"/> for a default-initialized <see cref="Either{T1, T2}"/>.
	/// </summary>
	/// <remarks>
	/// The exception names the generic type definition, because naming a constructed type would take the type
	/// arguments that the caller's shared code would have to look up.
	/// </remarks>
	/// <exception cref="EmptyStructException">Thrown when this method is called.</exception>
	[DoesNotReturn, StackTraceHidden]
	internal static void ThrowEmpty()
	{
		throw new EmptyStructException(nameof(Either<,>), typeof(Either<,>), innerException: null);
	}
}
