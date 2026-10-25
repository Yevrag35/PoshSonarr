using MG.Sonarr.Next.Buffers;

namespace MG.Sonarr.Next.Unions;

partial struct ObjEither<T1, T2>
{
	/// <summary>
	/// Match on the stored value and invoke the corresponding function.
	/// </summary>
	/// <remarks>
	/// <para>If this instance is <see langword="default"/>, this method returns <see langword="default"/> for <typeparamref name="TOutput"/>.</para>
	/// </remarks>
	/// <typeparam name="TOutput">The result type returned by the match functions.</typeparam>
	/// <param name="f1">The function to invoke when the stored value is a <typeparamref name="T1"/>.</param>
	/// <param name="f2">The function to invoke when the stored value is a <typeparamref name="T2"/>.</param>
	/// <returns>The value returned by the invoked function, or <see langword="default"/> when this instance is <see langword="default"/>.</returns>
	public TOutput Match<TOutput>(
		Func<T1, TOutput> f1,
		Func<T2, TOutput> f2)
	{
		return _index switch
		{
			1 => f1(Unsafe.As<T1>(_value)!),
			2 => f2(Unsafe.As<T2>(_value)!),
			_ => default!,
		};
	}
	/// <summary>
	/// Match on the stored value and invoke the corresponding function pointer wrapper.
	/// </summary>
	/// <remarks>
	/// <para>If this instance is <see langword="default"/>, this method returns <see langword="default"/> for <typeparamref name="TOutput"/>.</para>
	/// <para><b>Performance:</b> Uses <see cref="FnPtr{T, TResult}"/> wrappers to avoid delegate allocations.</para>
	/// </remarks>
	/// <typeparam name="TOutput">The result type returned by the match functions.</typeparam>
	/// <param name="f1">The function pointer wrapper to invoke when the stored value is a <typeparamref name="T1"/>.</param>
	/// <param name="f2">The function pointer wrapper to invoke when the stored value is a <typeparamref name="T2"/>.</param>
	/// <returns>The value returned by the invoked function, or <see langword="default"/> when this instance is <see langword="default"/>.</returns>
	public TOutput Match<TOutput>(
		FnPtr<T1, TOutput> f1,
		FnPtr<T2, TOutput> f2)
	{
		FnPtr.ThrowIfNull(f1);
		FnPtr.ThrowIfNull(f2);
		return _index switch
		{
			1 => f1 >> Unsafe.As<T1>(_value)!,
			2 => f2 >> Unsafe.As<T2>(_value)!,
			_ => default!,
		};
	}
	/// <summary>
	/// Match on the stored value and invoke the corresponding function with an additional state argument.
	/// </summary>
	/// <remarks>
	/// <para>If this instance is <see langword="default"/>, this method returns <see langword="default"/> for <typeparamref name="TOutput"/>.</para>
	/// <para>Type constraint: <typeparamref name="TState"/> allows <c>ref struct</c> usage, so state can be stack-only and cannot be boxed.</para>
	/// </remarks>
	/// <typeparam name="TState">The type of the state value passed to the match functions.</typeparam>
	/// <typeparam name="TOutput">The result type returned by the match functions.</typeparam>
	/// <param name="state">A state value passed to the invoked function.</param>
	/// <param name="f1">The function to invoke when the stored value is a <typeparamref name="T1"/>.</param>
	/// <param name="f2">The function to invoke when the stored value is a <typeparamref name="T2"/>.</param>
	/// <returns>The value returned by the invoked function, or <see langword="default"/> when this instance is <see langword="default"/>.</returns>
	public TOutput Match<TState, TOutput>(
		TState state,
		Func<TState, T1, TOutput> f1,
		Func<TState, T2, TOutput> f2) where TState : allows ref struct
	{
		return _index switch
		{
			1 => f1(state, Unsafe.As<T1>(_value)!),
			2 => f2(state, Unsafe.As<T2>(_value)!),
			_ => default!,
		};
	}
	/// <summary>
	/// Match on the stored value and invoke the corresponding function pointer wrapper with an additional state argument.
	/// </summary>
	/// <remarks>
	/// <para>If this instance is <see langword="default"/>, this method returns <see langword="default"/> for <typeparamref name="TOutput"/>.</para>
	/// <para>Type constraint: <typeparamref name="TState"/> allows <c>ref struct</c> usage, so state can be stack-only and cannot be boxed.</para>
	/// <para><b>Performance:</b> Uses function pointer wrappers to avoid delegate allocations.</para>
	/// </remarks>
	/// <typeparam name="TState">The type of the state value passed to the match functions.</typeparam>
	/// <typeparam name="TOutput">The result type returned by the match functions.</typeparam>
	/// <param name="state">A state value passed to the invoked function.</param>
	/// <param name="f1">The function pointer wrapper to invoke when the stored value is a <typeparamref name="T1"/>.</param>
	/// <param name="f2">The function pointer wrapper to invoke when the stored value is a <typeparamref name="T2"/>.</param>
	/// <returns>The value returned by the invoked function, or <see langword="default"/> when this instance is <see langword="default"/>.</returns>
	public TOutput Match<TState, TOutput>(
		TState state,
		FnPtr<TState, T1, TOutput> f1,
		FnPtr<TState, T2, TOutput> f2) where TState : allows ref struct
	{
		if (!f1.IsValid || !f2.IsValid)
		{
			Debug.Fail("One or both function pointer wrappers are invalid. Ensure that the function pointers are not null.");
            return default!;
		}

		return _index switch
		{
			1 => f1.Invoke(state, Unsafe.As<T1>(_value)!),
			2 => f2.Invoke(state, Unsafe.As<T2>(_value)!),
			_ => default!,
		};
	}
}
