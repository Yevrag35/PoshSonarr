namespace MG.Sonarr.Next.Buffers;

public readonly unsafe struct FnPtr<T0, T1, TResult>
	where T0 : allows ref struct
	where T1 : allows ref struct
{
	private readonly delegate* managed<T0, T1, TResult> _ptr;

	/// <summary>
	/// Gets a value indicating whether the current pointer is valid (i.e., has been initialized).
	/// </summary>
	public bool IsValid => _ptr is not null;

	/// <summary>
	/// Initializes a new instance of the <see cref="FnPtr{T0, T1, TResult}"/> struct wrapping the specified managed function pointer.
	/// </summary>
	/// <param name="ptr">The managed function pointer to be encapsulated. Cannot be null.</param>
	/// <exception cref="ArgumentNullException"><paramref name="ptr"/> is null.</exception>
	public FnPtr(delegate* managed<T0, T1, TResult> ptr)
	{
		ArgumentNullException.ThrowIfNull(ptr);
		_ptr = ptr;
	}

	/// <summary>
	/// Returns a pointer to the underlying function.
	/// </summary>
	/// <returns>A void pointer to the function.</returns>
	internal void* AsPointer()
	{
		return _ptr;
	}

	/// <summary>
	/// Invokes the encapsulated delegate with the specified arguments and returns its result.
	/// </summary>
	/// <param name="arg0">The first argument to pass to the delegate.</param>
	/// <param name="arg1">The second argument to pass to the delegate.</param>
	/// <returns>The result of type <typeparamref name="TResult"/> returned by the delegate after invocation.</returns>
	public TResult Invoke(T0 arg0, T1 arg1)
	{
		return _ptr(arg0, arg1);
	}
}

/// <summary>
/// Represents a strongly-typed managed function pointer with three parameters and a return value.
/// </summary>
/// <remarks>Use this struct to encapsulate and invoke managed function pointers with three parameters in a
/// type-safe manner. This is intended for advanced scenarios where direct function pointer invocation is required, performance-critical code.
/// The struct is immutable and does not own the lifetime of the underlying
/// function pointer.</remarks>
/// <typeparam name="T0">The type of the first parameter passed to the function. Must be a type that allows ref struct constraints.</typeparam>
/// <typeparam name="T1">The type of the second parameter passed to the function. Must be a type that allows ref struct constraints.</typeparam>
/// <typeparam name="T2">The type of the third parameter passed to the function. Must be a type that allows ref struct constraints.</typeparam>
/// <typeparam name="TResult">The type of the value returned by the function.</typeparam>
[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct FnPtr<T0, T1, T2, TResult>
    where T0 : allows ref struct
    where T1 : allows ref struct
    where T2 : allows ref struct
{
    private readonly delegate* managed<T0, T1, T2, TResult> _ptr;

	/// <summary>
	/// Gets a value indicating whether the current pointer is valid (i.e., has been initialized).
	/// </summary>
	[MemberNotNullWhen(true, nameof(_ptr))]
	public bool IsValid => _ptr is not null;

	/// <summary>
	/// Initializes a new instance of the <see cref="FnPtr{T0, T1, T2, TResult}"/> struct wrapping the specified managed function pointer.
	/// </summary>
	/// <param name="ptr">The managed function pointer to be encapsulated. Cannot be null.</param>
	/// <exception cref="ArgumentNullException"><paramref name="ptr"/> is null.</exception>
	public FnPtr(delegate* managed<T0, T1, T2, TResult> ptr)
    {
		ArgumentNullException.ThrowIfNull(ptr);
        _ptr = ptr;
    }

	/// <summary>
	/// Returns a pointer to the underlying function.
	/// </summary>
	/// <returns>A void pointer to the function.</returns>
	internal void* AsPointer()
	{
		return _ptr;
	}

	/// <summary>
	/// Invokes the encapsulated delegate with the specified arguments and returns its result.
	/// </summary>
	/// <param name="arg0">The first argument to pass to the delegate.</param>
	/// <param name="arg1">The second argument to pass to the delegate.</param>
	/// <param name="arg2">The third argument to pass to the delegate.</param>
	/// <returns>The result of type <typeparamref name="TResult"/> returned by the delegate after invocation.</returns>
	public TResult Invoke(T0 arg0, T1 arg1, T2 arg2)
    {
        return _ptr(arg0, arg1, arg2);
    }
}