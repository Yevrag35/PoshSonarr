using System.ComponentModel;

namespace MG.Sonarr.Next.Unions;

/// <summary>
/// Represents a union of three possible types.
/// </summary>
/// <typeparam name="T1">The first type.</typeparam>
/// <typeparam name="T2">The second type.</typeparam>
/// <typeparam name="T3">The third type.</typeparam>
[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay("{GetDebugString(),nq}")]
public readonly partial struct Either<T1, T2, T3>
{
    private readonly bool _isNotDefault;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly T1? _first;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly T2? _second;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly T3? _third;

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
    /// Gets the value of the third type if present.
    /// </summary>
    public readonly T3? AsT3 => _third;

    /// <summary>
    /// Gets the index of the current type.
    /// </summary>
    internal readonly uint Index => _index;

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
    /// Gets a value indicating whether the instance is of the third type.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_third), nameof(AsT3))]
    public readonly bool IsT3 => _index == 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="Either{T1, T2, T3}"/> struct with the first type.
    /// </summary>
    /// <param name="first">The value of the first type.</param>
    private Either(T1 first)
    {
        _isNotDefault = true;
        _first = first;
        _second = default;
        _third = default;
        _index = 1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Either{T1, T2, T3}"/> struct with the second type.
    /// </summary>
    /// <param name="second">The value of the second type.</param>
    private Either(T2 second)
    {
        _isNotDefault = true;
        _first = default;
        _second = second;
        _third = default;
        _index = 2;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Either{T1, T2, T3}"/> struct with the third type.
    /// </summary>
    /// <param name="third">The value of the third type.</param>
    private Either(T3 third)
    {
        _isNotDefault = true;
        _first = default;
        _second = default;
        _third = third;
        _index = 3;
    }

    /// <summary>
    /// Matches the current instance to one of the provided actions based on its type.
    /// </summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <param name="f1">The action to execute if the instance is of the first type.</param>
    /// <param name="f2">The action to execute if the instance is of the second type.</param>
    /// <param name="f3">The action to execute if the instance is of the third type.</param>
    /// <exception cref="EmptyStructException"></exception>
    public void Match<TState>(
        Action<T1> f1,
        Action<T2> f2,
        Action<T3> f3)
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

            case 3u:
                f3(_third!);
                break;

            default:
                throw new EmptyStructException(nameof(Either<T1, T2, T3>), this.GetType(), innerException: null);
        }
    }
    /// <summary>
    /// Matches the current instance to one of the provided actions based on its type, with state.
    /// </summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <param name="state">The state to pass to the actions.</param>
    /// <param name="f1">The action to execute if the instance is of the first type.</param>
    /// <param name="f2">The action to execute if the instance is of the second type.</param>
    /// <param name="f3">The action to execute if the instance is of the third type.</param>
    /// <exception cref="EmptyStructException"></exception>
    public void Match<TState>(
        TState state,
        Action<T1, TState> f1,
        Action<T2, TState> f2,
        Action<T3, TState> f3)// where TState : allows ref struct
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

            case 3u:
                f3(_third!, state);
                break;

            default:
                throw new EmptyStructException(nameof(Either<T1, T2, T3>), this.GetType(), innerException: null);
        }
    }

    /// <summary>
    /// Matches the current instance to one of the provided functions based on its type and returns the result.
    /// </summary>
    /// <typeparam name="TOutput">The output type.</typeparam>
    /// <param name="f1">The function to execute if the instance is of the first type.</param>
    /// <param name="f2">The function to execute if the instance is of the second type.</param>
    /// <param name="f3">The function to execute if the instance is of the third type.</param>
    /// <returns>The result of the executed function.</returns>
    /// <exception cref="EmptyStructException"></exception>
    public TOutput Match<TOutput>(
        Func<T1, TOutput> f1,
        Func<T2, TOutput> f2,
        Func<T3, TOutput> f3)
    {
        return _index switch
        {
            1u => f1(_first!),
            2u => f2(_second!),
            3u => f3(_third!),
            0u or _ => throw new EmptyStructException(nameof(Either<T1, T2, T3>), this.GetType(), innerException: null),
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
    /// <param name="f3">The function to execute if the instance is of the third type.</param>
    /// <returns>The result of the executed function.</returns>
    /// <exception cref="EmptyStructException"></exception>
    public TOutput Match<TOutput, TState>(
        TState state,
        Func<T1, TState, TOutput> f1,
        Func<T2, TState, TOutput> f2,
        Func<T3, TState, TOutput> f3)// where TState : allows ref struct
    {
        return _index switch
        {
            1u => f1(_first!, state),
            2u => f2(_second!, state),
            3u => f3(_third!, state),
            0u or _ => throw new EmptyStructException(nameof(Either<T1, T2, T3>), this.GetType(), innerException: null),
        };
    }

    /// <summary>
    /// Tries to get the value of the first type.
    /// </summary>
    /// <param name="t1">The value of the first type if present.</param>
    /// <param name="remaining">The remaining Either instance containing the second and third types.</param>
    /// <returns><see langword="true"/> if the instance is of the first type, otherwise <see langword="false"/>.</returns>
    /// <exception cref="EmptyStructException"></exception>
    public readonly bool TryGetT1([NotNullWhen(true)] out T1? t1, out Either<T2, T3> remaining)
    {
        EmptyStructException.ThrowIf(this.IsDefaultOrEmpty, this, nameof(Either<T1, T2, T3>));
        t1 = _first;
        remaining = Either.FromRemainingTwoAndThree(this);

        return this.IsT1;
    }
    /// <summary>
    /// Tries to get the value of the second type.
    /// </summary>
    /// <param name="t2">The value of the second type if present.</param>
    /// <param name="remaining">The remaining Either instance containing the first and third types.</param>
    /// <returns><see langword="true"/> if the instance is of the second type, otherwise <see langword="false"/>.</returns>
    /// <exception cref="EmptyStructException"></exception>
    public readonly bool TryGetT2([NotNullWhen(true)] out T2? t2, [NotNullWhen(false)] out Either<T1, T3> remaining)
    {
        EmptyStructException.ThrowIf(this.IsDefaultOrEmpty, this, nameof(Either<T1, T2, T3>));
        t2 = _second;
        remaining = Either.FromRemainingOneAndThree(this);

        return this.IsT2;
    }
    /// <summary>
    /// Tries to get the value of the third type.
    /// </summary>
    /// <param name="t3">The value of the third type if present.</param>
    /// <param name="remaining">The remaining Either instance containing the first and second types.</param>
    /// <returns><see langword="true"/> if the instance is of the third type, otherwise <see langword="false"/>.</returns>
    /// <exception cref="EmptyStructException"></exception>
    public readonly bool TryGetT3([NotNullWhen(true)] out T3? t3, [NotNullWhen(false)] out Either<T1, T2> remaining)
    {
        EmptyStructException.ThrowIf(this.IsDefaultOrEmpty, this, nameof(Either<T1, T2, T3>));
        t3 = _third;
        remaining = Either.FromRemainingOneAndTwo(this);

        return this.IsT3;
    }

#if DEBUG
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Don't use this method - only for debugging display.", error: true)]
    private string GetDebugString()
    {
        return _index switch
        {
            1u => $"T1: {_first}",
            2u => $"T2: {_second}",
            3u => $"T3: {_third}",
            0u or _ => "Invalid",
        };
    }
#endif

    public static Either<T1, T2, T3> FromT1(T1 value) => new(value);
    public static Either<T1, T2, T3> FromT2(T2 value) => new(value);
    public static Either<T1, T2, T3> FromT3(T3 value) => new(value);

    /// <summary>
    /// Implicitly converts a value of the first type to an Either instance.
    /// </summary>
    /// <param name="first">The value of the first type.</param>
    public static implicit operator Either<T1, T2, T3>(T1 first) => new(first);
    /// <summary>
    /// Implicitly converts a value of the second type to an Either instance.
    /// </summary>
    /// <param name="second">The value of the second type.</param>
    public static implicit operator Either<T1, T2, T3>(T2 second) => new(second);
    /// <summary>
    /// Implicitly converts a value of the third type to an Either instance.
    /// </summary>
    /// <param name="third">The value of the third type.</param>
    public static implicit operator Either<T1, T2, T3>(T3 third) => new(third);
}
