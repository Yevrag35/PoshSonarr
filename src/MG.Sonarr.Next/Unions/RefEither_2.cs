namespace MG.Sonarr.Next.Unions;

/// <summary>
/// Represents a discriminated union between <typeparamref name="T1"/> and <typeparamref name="T2"/> where only one value is present.
/// </summary>
/// <remarks>
/// This struct allows for by-ref types unlike <see cref="Either{T1, T2}"/>.
/// </remarks>
/// <typeparam name="T1">The first type.</typeparam>
/// <typeparam name="T2">The second type.</typeparam>
[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay(@"\{Index = {Index}\}")]
public readonly ref struct RefEither<T1, T2> where T1 : allows ref struct where T2 : allows ref struct
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly T1 _first;
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly T2 _second;
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly uint _index;

	/// <summary>
	/// Gets the value of the first type if present.
	/// </summary>
	public T1 AsT1 => _first;

	/// <summary>
	/// Gets the value of the second type if present.
	/// </summary>
	public T2 AsT2 => _second;

	/// <summary>
	/// The index of this <see cref="RefEither{T1, T2}"/> instance indicating which value is present.
	/// </summary>
	/// <remarks>
	/// A value of <c>0</c> indicates a default-initialized <see cref="RefEither{T1, T2}"/> instance.
	/// </remarks>
	public uint Index => _index;

	/// <summary>
	/// Gets a value indicating whether the instance is default or empty.
	/// </summary>
	public bool IsDefaultOrEmpty => _index == 0;

	/// <summary>
	/// Gets a value indicating whether the instance is of the first type - <typeparamref name="T1"/>.
	/// </summary>
	[MemberNotNullWhen(true, nameof(_first), nameof(AsT1))]
	public bool IsT1 => _index == 1;

	/// <summary>
	/// Gets a value indicating whether the instance is of the second type - <typeparamref name="T2"/>.
	/// </summary>
	[MemberNotNullWhen(true, nameof(_second), nameof(AsT2))]
	public bool IsT2 => _index == 2;

	/// <summary>
	/// Initializes a new instance of the <see cref="Either{T1, T2}"/> struct with the first type.
	/// </summary>
	/// <param name="first">The value of the first type.</param>
	private RefEither(T1 first)
	{
		_first = first;
		_second = default!;
		_index = 1;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Either{T1, T2}"/> struct with the second type.
	/// </summary>
	/// <param name="second">The value of the second type.</param>
	private RefEither(T2 second)
	{
		_first = default!;
		_second = second;
		_index = 2;
	}

	/// <summary>
	/// Matches the current instance to one of the provided functions based on its type and returns the result.
	/// </summary>
	/// <typeparam name="TOutput">The output type.</typeparam>
	/// <param name="f1">The function to execute if the instance is of the first type.</param>
	/// <param name="f2">The function to execute if the instance is of the second type.</param>
	/// <returns>The result of the executed function.</returns>
	/// <exception cref="EmptyStructException"></exception>
	public TOutput Match<TOutput>(
		Func<T1, TOutput> f1,
		Func<T2, TOutput> f2) where TOutput : allows ref struct
	{
		return _index switch
		{
			1 => f1(_first!),
			2 => f2(_second!),
			_ => default!,
		};
	}

	/// <summary>
	/// Tries to get the value of the first type.
	/// </summary>
	/// <param name="t1">The value of the first type if present.</param>
	/// <param name="t2">The value of the second type if present.</param>
	/// <returns><see langword="true"/> if the instance is of the first type, otherwise <see langword="false"/>.</returns>
	/// <exception cref="EmptyStructException"></exception>
	public bool TryGetT1([NotNullWhen(true)] out T1? t1, [NotNullWhen(false)] out T2? t2)
	{
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
	public bool TryGetT2([NotNullWhen(true)] out T2? t2, [NotNullWhen(false)] out T1? t1)
	{
		t1 = _first;
		t2 = _second;
		return this.IsT2;
	}

	[DebuggerStepThrough]
	public static implicit operator RefEither<T1, T2>(T1 first) => new(first);
	[DebuggerStepThrough]
	public static implicit operator RefEither<T1, T2>(T2 second) => new(second);
}