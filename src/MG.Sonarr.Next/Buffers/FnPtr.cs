namespace MG.Sonarr.Next.Buffers;

public static class FnPtr
{
	/// <summary>
	/// Throws an exception if the specified function pointer is not valid or has not been initialized.
	/// </summary>
	/// <typeparam name="T0">The type of the first parameter of the function pointer.</typeparam>
	/// <typeparam name="T1">The type of the second parameter of the function pointer.</typeparam>
	/// <typeparam name="T2">The type of the third parameter of the function pointer.</typeparam>
	/// <typeparam name="TResult">The return type of the function pointer.</typeparam>
	/// <param name="ptr">The function pointer to validate. Must be initialized and valid.</param>
	/// <param name="paramName">The name of the parameter to include in the exception message if validation fails. This is typically provided
	/// automatically and should not be set manually.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="ptr"/> is not valid or has not been initialized.</exception>
	[StackTraceHidden]
	public static unsafe void ThrowIfInvalid<T0, T1, TResult>(FnPtr<T0, T1, TResult> ptr, [CallerArgumentExpression(nameof(ptr))] string? paramName = null)
		where T0 : allows ref struct
		where T1 : allows ref struct
	{
		ArgumentNullException.ThrowIfNull(ptr.AsPointer(), paramName);
	}

	/// <summary>
	/// Throws an exception if the specified function pointer is not valid or has not been initialized.
	/// </summary>
	/// <typeparam name="T0">The type of the first parameter of the function pointer.</typeparam>
	/// <typeparam name="T1">The type of the second parameter of the function pointer.</typeparam>
	/// <typeparam name="T2">The type of the third parameter of the function pointer.</typeparam>
	/// <typeparam name="TResult">The return type of the function pointer.</typeparam>
	/// <param name="ptr">The function pointer to validate. Must be initialized and valid.</param>
	/// <param name="paramName">The name of the parameter to include in the exception message if validation fails. This is typically provided
	/// automatically and should not be set manually.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="ptr"/> is not valid or has not been initialized.</exception>
	public static unsafe void ThrowIfInvalid<T0, T1, T2, TResult>(FnPtr<T0, T1, T2, TResult> ptr, [CallerArgumentExpression(nameof(ptr))] string? paramName = null)
		where T0 : allows ref struct
		where T1 : allows ref struct
		where T2 : allows ref struct
	{
		ArgumentNullException.ThrowIfNull(ptr.AsPointer(), paramName);
	}
}
