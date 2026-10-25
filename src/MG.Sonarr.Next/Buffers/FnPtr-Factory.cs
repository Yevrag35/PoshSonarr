namespace MG.Sonarr.Next.Buffers;

/// <summary>
/// Factory helpers for creating lightweight managed function pointer wrappers.
/// </summary>
/// <remarks>
/// Use the <c>Create</c> overloads to construct strongly-typed
/// wrappers around C# function pointers (<c>delegate*</c>). These wrappers provide an API-friendly <c>Invoke</c>
/// method and implicit conversions to simplify call sites while keeping the underlying call via an unmanaged-like pointer.
/// </remarks>
[DebuggerStepThrough, SuppressMessage("Design", "MA0131", Justification = "Analyzer doesn't recognize that pointers have their own overload for ThrowIfNull.")]
public static class FnPtr
{
	/// <summary>
	/// Creates a <see cref="FnPtr{TResult}"/> wrapper for the provided parameterless managed function pointer.
	/// </summary>
	/// <typeparam name="TResult">The return type of the function pointer.</typeparam>
	/// <param name="ptr">A managed function pointer of shape <c>delegate* managed&lt;TResult&gt;</c>.</param>
	/// <returns>A <see cref="FnPtr{TResult}"/> that invokes the provided pointer.</returns>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static
#if !NET11_0_OR_GREATER
			unsafe
#endif
		FnPtr<TResult> Create<TResult>(delegate* managed<TResult> ptr)
	{
		unsafe { return new(ptr); }
	}

	/// <summary>
	/// Creates a <see cref="FnPtr{T, TOut}"/> wrapper for the provided managed function pointer.
	/// </summary>
	/// <typeparam name="T">The parameter type accepted by the function pointer. Typically a <see langword="ref"/>-like type such as a ref struct.</typeparam>
	/// <typeparam name="TResult">The return type of the function pointer.</typeparam>
	/// <param name="ptr">A managed function pointer of shape <c>delegate* managed&lt;T, TOut&gt;</c>.</param>
	/// <returns>A <see cref="FnPtr{T, TOut}"/> that invokes the provided pointer.</returns>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static
#if !NET11_0_OR_GREATER
			unsafe
#endif
		FnPtr<T, TResult> Create<T, TResult>(delegate* managed<T, TResult> ptr)
		where T : allows ref struct
	{
		unsafe { return new(ptr); }
	}

	/// <summary>
	/// Creates a <see cref="FnPtr{T1, T2, TOut}"/> wrapper for the provided managed function pointer with two input parameters.
	/// </summary>
	/// <typeparam name="T1">The first parameter type accepted by the function pointer.</typeparam>
	/// <typeparam name="T2">The second parameter type accepted by the function pointer.</typeparam>
	/// <typeparam name="TResult">The return type of the function pointer.</typeparam>
	/// <param name="ptr">A managed function pointer of shape <c>delegate* managed&lt;T1, T2, TOut&gt;</c>.</param>
	/// <returns>A <see cref="FnPtr{T1, T2, TOut}"/> that invokes the provided pointer.</returns>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static
#if !NET11_0_OR_GREATER
			unsafe
#endif
		FnPtr<T1, T2, TResult> Create<T1, T2, TResult>(delegate* managed<T1, T2, TResult> ptr)
		where T1 : allows ref struct
		where T2 : allows ref struct
	{
		unsafe { return new(ptr); }
	}

	/// <summary>
	/// Throws an exception if the specified function pointer is <see langword="null"/>.
	/// </summary>
	/// <typeparam name="TResult">The return type of the function pointer.</typeparam>
	/// <param name="ptr">The function pointer to validate.</param>
	/// <param name="paramName">An optional parameter name of <paramref name="ptr"/> to pass to the exception.</param>
	/// <exception cref="ArgumentNullException"><paramref name="ptr"/> has a null function pointer.</exception>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ThrowIfNull<TResult>(FnPtr<TResult> ptr, [CallerArgumentExpression(nameof(ptr))] string? paramName = null)
	{
		unsafe
		{
			delegate* managed<TResult> raw = ptr;
			ArgumentNullException.ThrowIfNull(raw, paramName);
		}
	}
	/// <summary>
	/// Throws an exception if the specified function pointer is <see langword="null"/>.
	/// </summary>
	/// <typeparam name="T">The type of the first parameter of the function pointer.</typeparam>
	/// <typeparam name="TResult">The return type of the function pointer.</typeparam>
	/// <param name="ptr">The function pointer to validate.</param>
	/// <param name="paramName">An optional parameter name of <paramref name="ptr"/> to pass to the exception.</param>
	/// <exception cref="ArgumentNullException"><paramref name="ptr"/> has a null function pointer.</exception>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ThrowIfNull<T, TResult>(FnPtr<T, TResult> ptr, [CallerArgumentExpression(nameof(ptr))] string? paramName = null)
		where T : allows ref struct
	{
		unsafe
		{
			delegate* managed<T, TResult> raw = ptr;
			ArgumentNullException.ThrowIfNull(raw, paramName);
		}
	}
	/// <summary>
	/// Throws an exception if the specified function pointer is <see langword="null"/>.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter of the function pointer.</typeparam>
	/// <typeparam name="T2">The type of the second parameter of the function pointer.</typeparam>
	/// <typeparam name="TResult">The return type of the function pointer.</typeparam>
	/// <param name="ptr">The function pointer to validate.</param>
	/// <param name="paramName">An optional parameter name of <paramref name="ptr"/> to pass to the exception.</param>
	/// <exception cref="ArgumentNullException"><paramref name="ptr"/> has a null function pointer.</exception>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ThrowIfNull<T1, T2, TResult>(FnPtr<T1, T2, TResult> ptr, [CallerArgumentExpression(nameof(ptr))] string? paramName = null)
		where T1 : allows ref struct
		where T2 : allows ref struct
	{
		unsafe
		{
			delegate* managed<T1, T2, TResult> raw = ptr;
			ArgumentNullException.ThrowIfNull(raw, paramName);
		}
	}
	/// <summary>
	/// Throws an exception if the specified function pointer is <see langword="null"/>.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter of the function pointer.</typeparam>
	/// <typeparam name="T2">The type of the second parameter of the function pointer.</typeparam>
	/// <typeparam name="T3">The type of the third parameter of the function pointer.</typeparam>
	/// <typeparam name="TResult">The return type of the function pointer.</typeparam>
	/// <param name="ptr">The function pointer to validate.</param>
	/// <param name="paramName">An optional parameter name of <paramref name="ptr"/> to pass to the exception.</param>
	/// <exception cref="ArgumentNullException"><paramref name="ptr"/> has a null function pointer.</exception>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ThrowIfNull<T1, T2, T3, TResult>(FnPtr<T1, T2, T3, TResult> ptr, [CallerArgumentExpression(nameof(ptr))] string? paramName = null)
		where T1 : allows ref struct
		where T2 : allows ref struct
		where T3 : allows ref struct
	{
		unsafe
		{
			delegate* managed<T1, T2, T3, TResult> raw = ptr;
			ArgumentNullException.ThrowIfNull(raw, paramName);
		}
	}

	/// <summary>
	/// Throws an exception if the specified function pointer is <see langword="null"/>.
	/// </summary>
	/// <typeparam name="T1">The type of the first parameter of the function pointer.</typeparam>
	/// <typeparam name="T2">The type of the second parameter of the function pointer.</typeparam>
	/// <typeparam name="T3">The type of the third parameter of the function pointer.</typeparam>
	/// <typeparam name="T4">The type of the fourth parameter of the function pointer.</typeparam>
	/// <typeparam name="TResult">The return type of the function pointer.</typeparam>
	/// <param name="ptr">The function pointer to validate.</param>
	/// <param name="paramName">An optional parameter name of <paramref name="ptr"/> to pass to the exception.</param>
	/// <exception cref="ArgumentNullException"><paramref name="ptr"/> has a null function pointer.</exception>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ThrowIfNull<T1, T2, T3, T4, TResult>(FnPtr<T1, T2, T3, T4, TResult> ptr, [CallerArgumentExpression(nameof(ptr))] string? paramName = null)
		where T1 : allows ref struct
		where T2 : allows ref struct
		where T3 : allows ref struct
		where T4 : allows ref struct
	{
		unsafe
		{
			delegate* managed<T1, T2, T3, T4, TResult> raw = ptr;
			ArgumentNullException.ThrowIfNull(raw, paramName);
		}
	}

	/// <summary>
	/// Throws an <see cref="EmptyStructException"/> if the specified <see cref="FnPtr{TResult}"/> is empty/default-initialized.
	/// </summary>
	/// <typeparam name="TResult">The return type of the function pointer.</typeparam>
	/// <param name="isEmpty">A boolean indicating whether the struct is empty/default-initialized. If <see langword="true"/>, an exception is thrown.</param>
	/// <param name="objectName">The name of the object being validated. Used in the exception message if validation fails.</param>
	/// <exception cref="EmptyStructException">Thrown if the specified <see cref="FnPtr{TResult}"/> is empty/default-initialized.</exception>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ThrowIfEmptyStruct<TResult>([DoesNotReturnIf(true)] bool isEmpty, string? objectName = null)
		where TResult : allows ref struct
	{
		if (isEmpty)
		{
			ThrowEmptyStruct<TResult>(objectName);
		}
	}
	[DoesNotReturn, DebuggerStepThrough, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowEmptyStruct<TResult>(string? objectName = null)
		where TResult : allows ref struct
	{
		throw new EmptyStructException(objectName, typeof(FnPtr<TResult>), innerException: null);
	}

	/// <summary>
	/// Throws an <see cref="EmptyStructException"/> if the specified <see cref="FnPtr{T, TResult}"/> is empty/default-initialized.
	/// </summary>
	/// <typeparam name="T">The input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
	/// <typeparam name="TResult">The return type of the function pointer.</typeparam>
	/// <param name="isEmpty">A boolean indicating whether the struct is empty/default-initialized. If <see langword="true"/>, an exception is thrown.</param>
	/// <param name="objectName">The name of the object being validated. Used in the exception message if validation fails.</param>
	/// <exception cref="EmptyStructException">Thrown if the specified <see cref="FnPtr{T, TResult}"/> is empty/default-initialized.</exception>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ThrowIfEmptyStruct<T, TResult>([DoesNotReturnIf(true)] bool isEmpty, string? objectName = null)
		where T : allows ref struct
		where TResult : allows ref struct
	{
		if (isEmpty)
		{
			ThrowEmptyStruct<T, TResult>(objectName);
		}
	}
	[DoesNotReturn, DebuggerStepThrough, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowEmptyStruct<T, TResult>(string? objectName = null)
		where T : allows ref struct
		where TResult : allows ref struct
	{
		throw new EmptyStructException(objectName, typeof(FnPtr<T, TResult>), innerException: null);
	}

	/// <summary>
	/// Throws an <see cref="EmptyStructException"/> if the specified <see cref="FnPtr{T1, T2, TResult}"/> is empty/default-initialized.
	/// </summary>
	/// <typeparam name="T1">The first input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
	/// <typeparam name="T2">The second input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
	/// <typeparam name="TResult">The return type of the function pointer.</typeparam>
	/// <param name="isEmpty">A boolean indicating whether the struct is empty/default-initialized. If <see langword="true"/>, an exception is thrown.</param>
	/// <param name="objectName">The name of the object being validated. Used in the exception message if validation fails.</param>
	/// <exception cref="EmptyStructException">Thrown if the specified <see cref="FnPtr{T1, T2, TResult}"/> is empty/default-initialized.</exception>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ThrowIfEmptyStruct<T1, T2, TResult>([DoesNotReturnIf(true)] bool isEmpty, string? objectName = null)
		where T1 : allows ref struct
		where T2 : allows ref struct
		where TResult : allows ref struct
	{
		if (isEmpty)
		{
			ThrowEmptyStruct<T1, T2, TResult>(objectName);
		}
	}
	[DoesNotReturn, DebuggerStepThrough, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowEmptyStruct<T1, T2, TResult>(string? objectName = null)
		where T1 : allows ref struct
		where T2 : allows ref struct
		where TResult : allows ref struct
	{
		throw new EmptyStructException(objectName, typeof(FnPtr<T1, T2, TResult>), innerException: null);
	}

	/// <summary>
	/// Throws an <see cref="EmptyStructException"/> if the specified <see cref="FnPtr{T1, T2, T3, TResult}"/> is empty/default-initialized.
	/// </summary>
	/// <typeparam name="T1">The first input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
	/// <typeparam name="T2">The second input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
	/// <typeparam name="T3">The third input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
	/// <typeparam name="TResult">The return type of the function pointer.</typeparam>
	/// <param name="isEmpty">A boolean indicating whether the struct is empty/default-initialized. If <see langword="true"/>, an exception is thrown.</param>
	/// <param name="objectName">The name of the object being validated. Used in the exception message if validation fails.</param>
	/// <exception cref="EmptyStructException">Thrown if the specified <see cref="FnPtr{T1, T2, T3, TResult}"/> is empty/default-initialized.</exception>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ThrowIfEmptyStruct<T1, T2, T3, TResult>([DoesNotReturnIf(true)] bool isEmpty, string? objectName = null)
		where T1 : allows ref struct
		where T2 : allows ref struct
		where T3 : allows ref struct
	{
		if (isEmpty)
		{
			ThrowEmptyStruct<T1, T2, T3, TResult>(objectName);
		}
	}
	[DoesNotReturn, DebuggerStepThrough, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowEmptyStruct<T1, T2, T3, TResult>(string? objectName = null)
		where T1 : allows ref struct
		where T2 : allows ref struct
		where T3 : allows ref struct
	{
		throw new EmptyStructException(objectName, typeof(FnPtr<T1, T2, T3, TResult>), innerException: null);
	}

	/// <summary>
	/// Throws an <see cref="EmptyStructException"/> if the specified <see cref="FnPtr{T1, T2, T3, T4, TResult}"/> is empty/default-initialized.
	/// </summary>
	/// <typeparam name="T1">The first input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
	/// <typeparam name="T2">The second input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
	/// <typeparam name="T3">The third input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
	/// <typeparam name="T4">The fourth input parameter type. Constrained to <see langword="ref"/>-like types via <c>allows ref struct</c>.</typeparam>
	/// <typeparam name="TResult">The return type of the function pointer.</typeparam>
	/// <param name="isEmpty">A boolean indicating whether the struct is empty/default-initialized. If <see langword="true"/>, an exception is thrown.</param>
	/// <param name="objectName">The name of the object being validated. Used in the exception message if validation fails.</param>
	/// <exception cref="EmptyStructException">Thrown if the specified <see cref="FnPtr{T1, T2, T3, T4, TResult}"/> is empty/default-initialized.</exception>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ThrowIfEmptyStruct<T1, T2, T3, T4, TResult>([DoesNotReturnIf(true)] bool isEmpty, string? objectName = null)
		where T1 : allows ref struct
		where T2 : allows ref struct
		where T3 : allows ref struct
		where T4 : allows ref struct
	{
		if (isEmpty)
		{
			ThrowEmptyStruct<T1, T2, T3, T4, TResult>(objectName);
		}
	}
	[DoesNotReturn, DebuggerStepThrough, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowEmptyStruct<T1, T2, T3, T4, TResult>(string? objectName = null)
		where T1 : allows ref struct
		where T2 : allows ref struct
		where T3 : allows ref struct
		where T4 : allows ref struct
	{
		throw new EmptyStructException(objectName, typeof(FnPtr<T1, T2, T3, T4, TResult>), innerException: null);
	}
}