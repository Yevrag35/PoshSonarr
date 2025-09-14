using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Unions;

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

    private readonly uint _index;

    /// <summary>
    /// Gets the value of the first type if present.
    /// </summary>
    public readonly T1? AsT1 => _first;

    /// <summary>
    /// Gets the value of the second type if present.
    /// </summary>
    public readonly T2? AsT2 => _second;

    /// <summary>
    /// Gets the index indicating the current type.
    /// </summary>
    public uint Index => _index;

    /// <summary>
    /// Gets a value indicating whether the instance is default or empty.
    /// </summary>
    public readonly bool IsDefaultOrEmpty => !_isNotDefault;

    /// <summary>
    /// Gets a value indicating whether the instance is of the first type.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_first), nameof(AsT1))]
    public readonly bool IsT1 => _index == 1;

    /// <summary>
    /// Gets a value indicating whether the instance is of the second type.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_second), nameof(AsT2))]
    public readonly bool IsT2 => _index == 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="Either{T1, T2}"/> struct with the specified values and index.
    /// </summary>
    /// <param name="first">The value of the first type.</param>
    /// <param name="second">The value of the second type.</param>
    /// <param name="index">The index indicating the current type.</param>
    internal Either(T1? first, T2? second, uint index)
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

    public readonly bool IsSameType(in Either<T1, T2> other)
    {
        return this.IsSameType(in other, ignoreDefaultOrEmpty: false);
    }
    public readonly bool IsSameType(in Either<T1, T2> other, bool ignoreDefaultOrEmpty)
    {
        return (other._index > 0u || ignoreDefaultOrEmpty) && _index == other._index;
    }
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
            case 0u:
                goto default;

            case 1u:
                f1(_first!);
                break;

            case 2u:
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
    /// <exception cref="EmptyStructException"></exception>
    public readonly object? MatchObj(
        object? state,
        Func<T1, object?, object?> f1,
        Func<T2, object?, object?> f2)
    {
        return _index switch
        {
            0u => throw new EmptyStructException(nameof(Either<,>), this.GetType(), innerException: null),
            1u => f1(_first!, state),
            2u => f2(_second!, state),
            _ => null,
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
            case 0u:
                goto default;

            case 1u:
                f1(_first!, state);
                break;

            case 2u:
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
    /// <exception cref="EmptyStructException"></exception>
    public readonly TOutput Match<TOutput>(
        Func<T1, TOutput> f1,
        Func<T2, TOutput> f2)
#if NET9_0_OR_GREATER
            where TOutput : allows ref struct
#endif
    {
        return _index switch
        {
            1u => f1(_first!),
            2u => f2(_second!),
            0u or _ => throw new EmptyStructException(nameof(Either<,>), this.GetType(), innerException: null),
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
    /// <exception cref="EmptyStructException"></exception>
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
            1u => f1(_first!, state),
            2u => f2(_second!, state),
            0u or _ => throw new EmptyStructException(nameof(Either<,>), this.GetType(), innerException: null),
        };
    }

    public unsafe TOutput Match<TOutput, TState>(
        TState state,
        delegate*<T1, TState, TOutput> f1,
        delegate*<T2, TState, TOutput> f2)
        where TState : allows ref struct
    {
        return _index switch
        {
            1 => f1(_first!, state),
            2 => f2(_second!, state),
            _ => default!,
        };
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
            case 0u:
                goto default;

            case 1u:
                f1(_first!, state);
                break;

            case 2u:
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
            case 0u:
                goto default;

            case 1u:
                f1(_first!, state);
                break;

            case 2u:
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

    public static Either<T1, T2> FromT1(T1 value) => new(value);
    public static Either<T1, T2> FromT2(T2 value) => new(value);

#if DEBUG
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Don't use this method - only for debugging display.")]
    private string GetDebugString()
    {
        return _index switch
        {
            1u => $"T1: {_first}",
            2u => $"T2: {_second}",
            0u or _ => "Invalid",
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
