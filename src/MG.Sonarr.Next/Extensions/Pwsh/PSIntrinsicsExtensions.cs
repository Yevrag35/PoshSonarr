namespace MG.Sonarr.Next.Extensions.Pwsh;

/// <summary>
/// Provides extension methods for <see cref="PSVariableIntrinsics"/>.
/// </summary>
public static class PSIntrinsicsExtensions
{
	/// <summary>
	/// Attempts to retrieve the value of a variable from the PowerShell variable intrinsics and cast it to the specified type.
	/// </summary>
	/// <typeparam name="T">The type to which the variable value should be cast.</typeparam>
	/// <param name="intrinsics">The PowerShell variable intrinsics.</param>
	/// <param name="variableName">The name of the variable to retrieve.</param>
	/// <param name="value">When this method returns, contains the value of the variable if it exists and can be cast to the specified type; otherwise, the default value for the type.</param>
	/// <returns><c>true</c> if the variable exists and can be cast to the specified type; otherwise, <c>false</c>.</returns>
	public static bool TryGetVariableValue<T>(this PSVariableIntrinsics intrinsics, string variableName, [NotNullWhen(true)] out T? value)
	{
		ArgumentNullException.ThrowIfNull(intrinsics);
		ArgumentException.ThrowIfNullOrEmpty(variableName);

		object? val;
		try
		{
			val = intrinsics.GetValue(variableName);
		}
		catch (Exception e)
		{
			Debug.Fail(e.Message);
			value = default;
			return false;
		}

		if (val is T tVal)
		{
			value = tVal;
			return true;
		}
		else
		{
			value = default;
			return false;
		}
	}
}
