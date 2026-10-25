namespace MG.Sonarr.Next.Unions;

/// <summary>
/// Provides static helper methods for working with <see cref="ObjEither{T1, T2}"/> union types.
/// </summary>
/// <remarks>The <see cref="ObjEither"/> class contains utility methods for validating and operating on
/// <see cref="ObjEither{T1, T2}"/> instances. 
/// These methods are intended to simplify common tasks when handling union values that may represent one of
/// two reference types or neither.</remarks>
[DebuggerStepThrough]
public static class ObjEither
{
	/// <summary>
	/// Throws an exception if the specified union does not contain a value of either type.
	/// </summary>
	/// <typeparam name="T1">The first reference type that the union may hold.</typeparam>
	/// <typeparam name="T2">The second reference type that the union may hold.</typeparam>
	/// <param name="value">The union value to validate. Must not be in the 'neither' state.</param>
	/// <param name="paramName">The name of the parameter to include in the exception message. This value is typically provided automatically and
	/// should not be set explicitly.</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="value"/> does not contain a value of either type.</exception>
	public static void ThrowIfNeither<T1, T2>(ObjEither<T1, T2> value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
		where T1 : class
		where T2 : class
	{
		if (value.IsNeither)
		{
			throw new ArgumentException("The union does not hold a valid value of either type.", paramName);
		}
	}
}
