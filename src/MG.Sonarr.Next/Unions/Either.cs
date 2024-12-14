namespace MG.Sonarr.Next.Unions;

/// <summary>
/// Provides static methods to create Either instances from remaining types.
/// </summary>
internal static class Either
{
	/// <summary>
	/// Creates an Either instance from the first and third types of the given Either.
	/// </summary>
	/// <typeparam name="T1">The first type.</typeparam>
	/// <typeparam name="T2">The second type.</typeparam>
	/// <typeparam name="T3">The third type.</typeparam>
	/// <param name="remaining">The original Either instance.</param>
	/// <returns>A new Either instance containing the first and third types.</returns>
	internal static Either<T1, T3> FromRemainingOneAndThree<T1, T2, T3>(Either<T1, T2, T3> remaining)
	{
		return new(remaining.AsT1, remaining.AsT3, remaining.Index);
	}

	/// <summary>
	/// Creates an Either instance from the second and third types of the given Either.
	/// </summary>
	/// <typeparam name="T1">The first type.</typeparam>
	/// <typeparam name="T2">The second type.</typeparam>
	/// <typeparam name="T3">The third type.</typeparam>
	/// <param name="remaining">The original Either instance.</param>
	/// <returns>A new Either instance containing the second and third types.</returns>
	internal static Either<T2, T3> FromRemainingTwoAndThree<T1, T2, T3>(Either<T1, T2, T3> remaining)
	{
		return new(remaining.AsT2, remaining.AsT3, remaining.Index);
	}

	/// <summary>
	/// Creates an Either instance from the first and second types of the given Either.
	/// </summary>
	/// <typeparam name="T1">The first type.</typeparam>
	/// <typeparam name="T2">The second type.</typeparam>
	/// <typeparam name="T3">The third type.</typeparam>
	/// <param name="remaining">The original Either instance.</param>
	/// <returns>A new Either instance containing the first and second types.</returns>
	internal static Either<T1, T2> FromRemainingOneAndTwo<T1, T2, T3>(Either<T1, T2, T3> remaining)
	{
		return new(remaining.AsT1, remaining.AsT2, remaining.Index);
	}
}