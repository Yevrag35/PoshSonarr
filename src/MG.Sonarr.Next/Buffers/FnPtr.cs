namespace MG.Sonarr.Next.Buffers;

/// <summary>
/// A lightweight wrapper around a managed function pointer that returns <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The return type of the pointer.</typeparam>
[StructLayout(LayoutKind.Auto), DebuggerStepThrough, DebuggerDisplay("{ToString(),nq}")]
[SuppressMessage("Design", "MA0131", Justification = "Analyzer doesn't recognize that pointers have their own overload for ThrowIfNull.")]
public readonly struct FnPtr<TResult> where TResult : allows ref struct
{
	private readonly
#if !NET11_0_OR_GREATER
			unsafe
#endif
		delegate* managed<TResult> _ptr;

#pragma warning disable format
	/// <summary>
	/// Gets a value indicating whether the current instance is valid.
	/// </summary>
	[MemberNotNullWhen(true, nameof(_ptr))]
	public bool IsValid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get { unsafe { return _ptr is not null; } }
	}
#pragma warning restore format
	/// <summary>
	/// Initializes a new instance that wraps the specified function pointer.
	/// </summary>
	/// <param name="ptr">The managed function pointer to wrap.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="ptr"/> is null.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public
#if !NET11_0_OR_GREATER
			unsafe
#endif
		FnPtr(delegate* managed<TResult> ptr)
	{
		unsafe {
			ArgumentNullException.ThrowIfNull(ptr);
			_ptr = ptr;
		}
	}

	/// <summary>
	/// Invokes the underlying delegate and returns the result.
	/// </summary>
	/// <returns>The result produced by invoking the delegate.</returns>
	/// <exception cref="EmptyStructException">Thrown if this instance is empty/default-initialized.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TResult Invoke()
	{
		unsafe
		{
			FnPtr.ThrowIfEmptyStruct<TResult>(_ptr is null, nameof(_ptr));
			return _ptr();
		}
	}

	/// <summary>
	/// Invokes the function pointer represented by the specified <see cref="FnPtr{TResult}"/> instance using the
	/// right-shift operator.
	/// </summary>
	/// <remarks>This operator provides a syntactic shortcut for invoking the function pointer. The second parameter
	/// is not used and can be any value.</remarks>
	/// <param name="fnPtr">The function pointer to invoke.</param>
	/// <param name="_">A placeholder parameter that is ignored during invocation.</param>
	/// <returns>The result returned by the function pointer invocation.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TResult operator >>(FnPtr<TResult> fnPtr, object? _)
	{
		return fnPtr.Invoke();
	}
	/// <summary>
	/// Implicitly converts a raw managed function pointer to a <see cref="FnPtr{TResult}"/> wrapper.
	/// </summary>
	/// <param name="ptr">The managed function pointer to convert.</param>
	public static
#if !NET11_0_OR_GREATER
			unsafe
#endif
		implicit operator FnPtr<TResult>(delegate* managed<TResult> ptr)
	{
		unsafe { return new(ptr); }
	}

	/// <summary>
	/// Implicitly converts a <see cref="FnPtr{TResult}"/> wrapper to a raw managed function pointer.
	/// </summary>
	public static
#if !NET11_0_OR_GREATER
			unsafe
#endif
		implicit operator delegate* managed<TResult>(FnPtr<TResult> ptr)
	{
		unsafe { return ptr._ptr; }
	}

#pragma warning disable format
	/// <summary>
	/// Returns a string that represents the current object, including its validity and result type information.
	/// </summary>
	/// <returns>A string containing the pointer value if the object is valid, or "&lt;invalid&gt;" if not, followed by the type of the
	/// result.
	/// <para>
	/// <code>&amp;{&lt;pointer&gt;) () -> "<typeparamref name="TResult"/>"</code>
	/// </para>
	/// </returns>
	public override string ToString()
	{
		const string format = "&{{{0}}} () -> {1}";
		object value;
		unsafe { value = this.IsValid ? (nint)_ptr : "<invalid>"; }

		return string.Format(CultureInfo.CurrentCulture, format, value, typeof(TResult));
	}
#pragma warning restore format
}

/// <summary>
/// A lightweight wrapper around a managed function pointer that accepts a single (1) <typeparamref name="T"/> argument and returns <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="T">The input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
/// <typeparam name="TResult">The return type of the pointer.</typeparam>
[StructLayout(LayoutKind.Auto), DebuggerStepThrough, DebuggerDisplay("{ToString(),nq}")]
[SuppressMessage("Design", "MA0131", Justification = "Analyzer doesn't recognize that pointers have their own overload for ThrowIfNull.")]
public readonly struct FnPtr<T, TResult> where T : allows ref struct where TResult : allows ref struct
{
	private readonly
#if !NET11_0_OR_GREATER
			unsafe
#endif
		delegate* managed<T, TResult> _ptr;

#pragma warning disable format
	/// <summary>
	/// Gets a value indicating whether the current instance is valid.
	/// </summary>
	public bool IsValid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get { unsafe { return _ptr is not null; } }
	}
#pragma warning restore format
	/// <summary>
	/// Initializes a new instance that wraps the function pointer at the specified raw address.
	/// </summary>
	/// <param name="ptr">The raw address of the managed function pointer to wrap.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="ptr"/> is null.</exception>
	internal FnPtr(nint ptr)
	{
		unsafe
		{
			_ptr = (delegate* managed<T, TResult>)ptr;
			ArgumentNullException.ThrowIfNull(_ptr, nameof(ptr));
		}
	}

	/// <summary>
	/// Initializes a new instance that wraps the specified function pointer.
	/// </summary>
	/// <param name="ptr">The managed function pointer to wrap.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="ptr"/> is null.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public
#if !NET11_0_OR_GREATER
			unsafe
#endif
		FnPtr(delegate* managed<T, TResult> ptr)
	{
		unsafe
		{
			ArgumentNullException.ThrowIfNull(ptr);
			_ptr = ptr;
		}
	}

	/// <summary>
	/// Invokes the underlying delegate with the specified argument and returns the result.
	/// </summary>
	/// <param name="arg">The input value to pass to the delegate. Cannot be null.</param>
	/// <returns>The result produced by invoking the delegate with the specified argument.</returns>
	/// <exception cref="EmptyStructException">Thrown if this instance is empty/default-initialized.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TResult Invoke(T arg)
	{
		unsafe
		{
			FnPtr.ThrowIfEmptyStruct<T, TResult>(_ptr is null, nameof(_ptr));
			return _ptr(arg);
		}
	}

	/// <summary>
	/// Invokes the function pointer with the specified argument and returns the result.
	/// </summary>
	/// <remarks>This operator provides a convenient syntax for invoking a function pointer using the right-shift
	/// operator. It is equivalent to calling the function pointer directly with the specified argument.</remarks>
	/// <param name="fnPtr">The function pointer to invoke. Must not be null.</param>
	/// <param name="arg">The argument to pass to the function pointer.</param>
	/// <returns>The result returned by the invoked function pointer.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TResult operator >>(FnPtr<T, TResult> fnPtr, T arg)
	{
		return fnPtr.Invoke(arg);
	}
	/// <summary>
	/// Implicitly converts a raw managed function pointer to a <see cref="FnPtr{T, TResult}"/> wrapper.
	/// </summary>
	/// <param name="ptr">The managed function pointer to convert.</param>
	public static
#if !NET11_0_OR_GREATER
			unsafe
#endif
		implicit operator FnPtr<T, TResult>(delegate* managed<T, TResult> ptr)
	{
		unsafe { return new(ptr); }
	}
	/// <summary>
	/// Implicitly converts a <see cref="FnPtr{T, TResult}"/> wrapper to a raw managed function pointer.
	/// </summary>
	public static
#if !NET11_0_OR_GREATER
			unsafe
#endif
		implicit operator delegate* managed<T, TResult>(FnPtr<T, TResult> ptr)
	{
		unsafe { return ptr._ptr; }
	}

#pragma warning disable format
	/// <summary>
	/// Returns a string that represents the current object, including its validity, input parameter type, and result type information.
	/// </summary>
	/// <returns>A string containing the signature of the function pointer if the object is valid, or "&lt;invalid&gt;" if not.
	/// <para>
	/// <code>&amp;{&lt;pointer&gt;) (<typeparamref name="T"/> x) -> "<typeparamref name="TResult"/>"</code>
	/// </para>
	/// </returns>
	public override string ToString()
	{
		const string format = "&{{{0}}} ({1} x) -> {2}";
		object value;
		unsafe { value = this.IsValid ? (nint)_ptr : "<invalid>"; }

		return string.Format(CultureInfo.CurrentCulture, format, value, typeof(T), typeof(TResult));
	}
#pragma warning restore format
}

/// <summary>
/// A lightweight wrapper around a managed function pointer that accepts two (2) parameters and returns a result.
/// </summary>
/// <typeparam name="T1">The first input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
/// <typeparam name="T2">The second input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
/// <typeparam name="TResult">The return type of the pointer.</typeparam>
[StructLayout(LayoutKind.Auto), DebuggerStepThrough, DebuggerDisplay("{ToString(),nq}")]
[SuppressMessage("Design", "MA0131", Justification = "Analyzer doesn't recognize that pointers have their own overload for ThrowIfNull.")]
public readonly struct FnPtr<T1, T2, TResult>
	where T1 : allows ref struct
	where T2 : allows ref struct
	where TResult : allows ref struct
{
	private readonly
#if !NET11_0_OR_GREATER
			unsafe
#endif
		delegate* managed<T1, T2, TResult> _ptr;

#pragma warning disable format
	/// <summary>
	/// Gets a value indicating whether the current instance is valid.
	/// </summary>
	public bool IsValid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get { unsafe { return _ptr is not null; } }
	}
#pragma warning restore format
	/// <summary>
	/// Initializes a new instance that wraps the specified function pointer.
	/// </summary>
	/// <param name="ptr">The managed function pointer to wrap.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="ptr"/> is null.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public
#if !NET11_0_OR_GREATER
			unsafe
#endif
		FnPtr(delegate* managed<T1, T2, TResult> ptr)
	{
		unsafe
		{
			ArgumentNullException.ThrowIfNull(ptr);
			_ptr = ptr;
		}
	}
	/// <summary>
	/// Invokes the underlying function pointer with the given arguments.
	/// </summary>
	/// <param name="arg1">The first argument to pass to the function pointer.</param>
	/// <param name="arg2">The second argument to pass to the function pointer.</param>
	/// <returns>The value returned by the function pointer.</returns>
	/// <exception cref="EmptyStructException">Thrown if this instance is empty/default-initialized.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TResult Invoke(T1 arg1, T2 arg2)
	{
		unsafe
		{
			FnPtr.ThrowIfEmptyStruct<T1, T2, TResult>(_ptr is null, nameof(_ptr));
			return _ptr(arg1, arg2);
		}
	}

	/// <summary>
	/// Implicitly converts a raw managed function pointer to a <see cref="FnPtr{T1, T2, TOut}"/> wrapper.
	/// </summary>
	/// <param name="ptr">The managed function pointer to convert.</param>
	public static
#if !NET11_0_OR_GREATER
			unsafe
#endif
		implicit operator FnPtr<T1, T2, TResult>(delegate* managed<T1, T2, TResult> ptr)
	{
		unsafe { return new(ptr); }
	}
	/// <summary>
	/// Implicitly converts a <see cref="FnPtr{T1, T2, TOut}"/> wrapper to a raw managed function pointer.
	/// </summary>
	public static
#if !NET11_0_OR_GREATER
			unsafe
#endif
		implicit operator delegate* managed<T1, T2, TResult>(FnPtr<T1, T2, TResult> ptr)
	{
		unsafe { return ptr._ptr; }
	}

#pragma warning disable format
	/// <summary>
	/// Returns a string that represents the current object, including its validity, input parameter types, and result type information.
	/// </summary>
	/// <returns>A string containing the signature of the function pointer if the object is valid, or "&lt;invalid&gt;" if not.
	/// <para>
	/// <code>&amp;{&lt;pointer&gt;) (<typeparamref name="T1"/> x1, <typeparamref name="T2"/> x2) -> "<typeparamref name="TResult"/>"</code>
	/// </para>
	/// </returns>
	public override string ToString()
	{
		const string format = "&{{{0}}} ({1} x1, {2} x2) -> {3}";
		object value;
		unsafe { value = this.IsValid ? (nint)_ptr : "<invalid>"; }

		return string.Format(CultureInfo.CurrentCulture, format, value, typeof(T1), typeof(T2), typeof(TResult));
	}
#pragma warning restore format
}

/// <summary>
/// A lightweight wrapper around a managed function pointer that accepts three (3) parameters and returns a result.
/// </summary>
/// <typeparam name="T1">The first input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
/// <typeparam name="T2">The second input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
/// <typeparam name="T3">The third input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
/// <typeparam name="TResult">The return type of the pointer.</typeparam>
[StructLayout(LayoutKind.Auto), DebuggerStepThrough, DebuggerDisplay("{ToString(),nq}")]
[SuppressMessage("Design", "MA0131", Justification = "Analyzer doesn't recognize that pointers have their own overload for ThrowIfNull.")]
public readonly struct FnPtr<T1, T2, T3, TResult>
	where T1 : allows ref struct
	where T2 : allows ref struct
	where T3 : allows ref struct
{
	private readonly
#if !NET11_0_OR_GREATER
			unsafe
#endif
		delegate* managed<T1, T2, T3, TResult> _ptr;

#pragma warning disable format
	/// <summary>
	/// Gets a value indicating whether the current instance is valid.
	/// </summary>
	public bool IsValid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get { unsafe { return _ptr is not null; } }
	}
#pragma warning restore format
	/// <summary>
	/// Initializes a new instance that wraps the specified function pointer.
	/// </summary>
	/// <param name="ptr">The managed function pointer to wrap.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="ptr"/> is null.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public
#if !NET11_0_OR_GREATER
			unsafe
#endif
		FnPtr(delegate* managed<T1, T2, T3, TResult> ptr)
	{
		unsafe
		{
			ArgumentNullException.ThrowIfNull(ptr);
			_ptr = ptr;
		}
	}
	/// <summary>
	/// Invokes the underlying function pointer with the given arguments.
	/// </summary>
	/// <param name="arg0">The first argument to pass to the function pointer.</param>
	/// <param name="arg1">The second argument to pass to the function pointer.</param>
	/// <param name="arg2">The third argument to pass to the function pointer.</param>
	/// <returns>The value returned by the function pointer.</returns>
	/// <exception cref="EmptyStructException">Thrown if this instance is empty/default-initialized.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TResult Invoke(T1 arg0, T2 arg1, T3 arg2)
	{
		unsafe
		{
			FnPtr.ThrowIfEmptyStruct<T1, T2, T3, TResult>(_ptr is null, nameof(_ptr));
			return _ptr(arg0, arg1, arg2);
		}
	}

	/// <summary>
	/// Implicitly converts a raw managed function pointer to a <see cref="FnPtr{T1, T2, T3, TOut}"/> wrapper.
	/// </summary>
	/// <param name="ptr">The managed function pointer to convert.</param>
	public static
#if !NET11_0_OR_GREATER
			unsafe
#endif
		implicit operator FnPtr<T1, T2, T3, TResult>(delegate* managed<T1, T2, T3, TResult> ptr)
	{
		unsafe { return new(ptr); }
	}
	/// <summary>
	/// Implicitly converts a <see cref="FnPtr{T1, T2, T3, TOut}"/> wrapper to a raw managed function pointer.
	/// </summary>
	/// <param name="ptr">The managed function pointer to convert.</param>
	public static
#if !NET11_0_OR_GREATER
			unsafe
#endif
		implicit operator delegate* managed<T1, T2, T3, TResult>(FnPtr<T1, T2, T3, TResult> ptr)
	{
		unsafe { return ptr._ptr; }
	}

#pragma warning disable format
	/// <summary>
	/// Returns a string that represents the current object, including its validity, input parameter types, and result type information.
	/// </summary>
	/// <returns>A string containing the signature of the function pointer if the object is valid, or "&lt;invalid&gt;" if not.
	/// <para>
	/// <code>&amp;{&lt;pointer&gt;) (<typeparamref name="T1"/> x1, <typeparamref name="T2"/> x2, <typeparamref name="T3"/> x3) -> "<typeparamref name="TResult"/>"</code>
	/// </para>
	/// </returns>
	public override string ToString()
	{
		const string format = "&{{{0}}} ({1} x1, {2} x2, {3} x3) -> {4}";
		object value;
		unsafe { value = this.IsValid ? (nint)_ptr : "<invalid>"; }

		return string.Format(CultureInfo.CurrentCulture, format, value, typeof(T1), typeof(T2), typeof(T3), typeof(TResult));
	}
#pragma warning restore format
}

/// <summary>
/// A lightweight wrapper around a managed function pointer that accepts four (4) parameters and returns a result.
/// </summary>
/// <typeparam name="T1">The first input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
/// <typeparam name="T2">The second input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
/// <typeparam name="T3">The third input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
/// <typeparam name="T4">The fourth input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
/// <typeparam name="TResult">The return type of the pointer.</typeparam>
[StructLayout(LayoutKind.Auto), DebuggerStepThrough, DebuggerDisplay("{ToString(),nq}")]
[SuppressMessage("Design", "MA0131", Justification = "Analyzer doesn't recognize that pointers have their own overload for ThrowIfNull.")]
public readonly struct FnPtr<T1, T2, T3, T4, TResult>
	where T1 : allows ref struct
	where T2 : allows ref struct
	where T3 : allows ref struct
	where T4 : allows ref struct
{
	private readonly
#if !NET11_0_OR_GREATER
			unsafe
#endif
		delegate* managed<T1, T2, T3, T4, TResult> _ptr;

#pragma warning disable format
	/// <summary>
	/// Gets a value indicating whether the current instance is valid.
	/// </summary>
	public bool IsValid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get { unsafe { return _ptr is not null; } }
	}
#pragma warning restore format
	/// <summary>
	/// Initializes a new instance that wraps the specified function pointer.
	/// </summary>
	/// <param name="ptr">The managed function pointer to wrap.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="ptr"/> is null.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public
#if !NET11_0_OR_GREATER
			unsafe
#endif
		FnPtr(delegate* managed<T1, T2, T3, T4, TResult> ptr)
	{
		unsafe
		{
			ArgumentNullException.ThrowIfNull(ptr);
			_ptr = ptr;
		}
	}
	/// <summary>
	/// Invokes the underlying function pointer with the given arguments.
	/// </summary>
	/// <param name="arg1">The first argument to pass to the function pointer.</param>
	/// <param name="arg2">The second argument to pass to the function pointer.</param>
	/// <param name="arg3">The third argument to pass to the function pointer.</param>
	/// <param name="arg4">The fourth argument to pass to the function pointer.</param>
	/// <returns>The value returned by the function pointer.</returns>
	/// <exception cref="EmptyStructException">Thrown if this instance is empty/default-initialized.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TResult Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		unsafe
		{
			FnPtr.ThrowIfEmptyStruct<T1, T2, T3, T4, TResult>(_ptr is null, nameof(_ptr));
			return _ptr(arg1, arg2, arg3, arg4);
		}
	}

	/// <summary>
	/// Implicitly converts a raw managed function pointer to a <see cref="FnPtr{T1, T2, T3, T4, TResult}"/> wrapper.
	/// </summary>
	/// <param name="ptr">The managed function pointer to convert.</param>
	public static
#if !NET11_0_OR_GREATER
			unsafe
#endif
		implicit operator FnPtr<T1, T2, T3, T4, TResult>(delegate* managed<T1, T2, T3, T4, TResult> ptr)
	{
		unsafe { return new(ptr); }
	}
	/// <summary>
	/// Implicitly converts a <see cref="FnPtr{T1, T2, T3, T4, TResult}"/> wrapper to a raw managed function pointer.
	/// </summary>
	/// <param name="ptr">The managed function pointer to convert.</param>
	public static
#if !NET11_0_OR_GREATER
			unsafe
#endif
		implicit operator delegate* managed<T1, T2, T3, T4, TResult>(FnPtr<T1, T2, T3, T4, TResult> ptr)
	{
		unsafe { return ptr._ptr; }
	}
#pragma warning disable format
	/// <summary>
	/// Returns a string that represents the current object, including its validity, input parameter types, and result type information.
	/// </summary>
	/// <returns>A string containing the signature of the function pointer if the object is valid, or "&lt;invalid&gt;" if not.
	/// <para>
	/// <code>&amp;{&lt;pointer&gt;) (<typeparamref name="T1"/> x1, <typeparamref name="T2"/> x2, <typeparamref name="T3"/> x3, <typeparamref name="T4"/> x4) -> "<typeparamref name="TResult"/>"</code>
	/// </para>
	/// </returns>
	public override string ToString()
	{
		const string format = "&{{{0}}} ({1} x1, {2} x2, {3} x3, {4} x4) -> {5}";
		object value;
		unsafe { value = this.IsValid ? (nint)_ptr : "<invalid>"; }

		return string.Format(CultureInfo.CurrentCulture, format, value, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(TResult));
	}
#pragma warning restore format
}