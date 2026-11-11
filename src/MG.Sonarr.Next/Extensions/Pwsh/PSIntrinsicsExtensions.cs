namespace MG.Sonarr.Next.Extensions.Pwsh;

/// <summary>
/// Provides extension methods for <see cref="PSVariableIntrinsics"/>.
/// </summary>
internal static class PSIntrinsicsExtensions
{
	/// <summary>
	/// Gets the part of a property name after its last period, or the whole name when it has none.
	/// </summary>
	/// <param name="intrinsics">The <see cref="PSVariableIntrinsics"/> instance being extended.</param>
	extension(PSVariableIntrinsics intrinsics)
	{
		/// <summary>
		/// Attempts to retrieve the value of a variable from the PowerShell variable intrinsics and cast it to the specified type.
		/// </summary>
		/// <typeparam name="T">The type to which the variable value should be cast.</typeparam>
		/// <param name="variableName">The name of the variable to retrieve.</param>
		/// <param name="value">When this method returns, contains the value of the variable if it exists and can be cast to the specified type; otherwise, the default value for the type.</param>
		/// <returns><c>true</c> if the variable exists and can be cast to the specified type; otherwise, <c>false</c>.</returns>
		internal bool TryGetVariableValue<T>(string variableName, [NotNullWhen(true)] out T? value)
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
}
