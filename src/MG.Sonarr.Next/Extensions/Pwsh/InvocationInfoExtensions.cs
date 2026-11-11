using MG.Sonarr.Next.Collections.Intrinsics;
using MG.Sonarr.Next.Extensions.Strings;

namespace MG.Sonarr.Next.Extensions.Pwsh;

/// <summary>
/// Provides extension methods for <see cref="InvocationInfo"/>.
/// </summary>
internal static class InvocationInfoExtensions
{
	/// <summary>
	/// Determines whether a parameter is bound positionally in the invocation.
	/// </summary>
	/// <param name="invocation">The <see cref="InvocationInfo"/> instance being extended.</param>
	extension(InvocationInfo invocation)
	{
		/// <summary>
		/// Determines whether a parameter is bound positionally in the invocation.
		/// </summary>
		/// <remarks>
		/// This method is generic to allow for value types to be passed without boxing.
		/// </remarks>
		/// <typeparam name="T">The type of the parameter value.</typeparam>
		/// <param name="value">The parameter value.</param>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <returns><see langword="true"/> if the parameter is bound positionally; otherwise, <see langword="false"/>.</returns>
		[DebuggerStepThrough]
		internal bool IsBoundPositionally<T>(T value, [CallerArgumentExpression(nameof(value))] string parameterName = "") where T : struct
		{
			return IsBoundPositionally(invocation, parameterName);
		}
		/// <summary>
		/// Determines whether a parameter is bound positionally in the invocation.
		/// </summary>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <returns><see langword="true"/> if the parameter is bound positionally; otherwise, <see langword="false"/>.</returns>
		internal bool IsBoundPositionally(string parameterName)
		{
			if (string.IsNullOrEmpty(parameterName)) return false;

			ReadOnlySpan<char> pName = FormatParameterName(parameterName);
			List<string> bound = GetBoundPositionList(invocation.BoundParameters);

			foreach (ReadOnlySpan<char> boundName in ListMarshal.AsReadOnlySpan(bound))
			{
				if (pName.Equals(boundName, StringComparison.Ordinal))
				{
					return true;
				}
			}

			return false;
		}
	}

	/// <summary>
	/// Gets the part of a property name after its last period, or the whole name when it has none.
	/// </summary>
	/// <remarks>
	/// A name that ends in a period is returned whole.
	/// </remarks>
	/// <param name="parameterName">The property name, usually an expression such as <c>value.Name</c>.</param>
	/// <returns>A slice of <paramref name="parameterName"/>.</returns>
	private static ReadOnlySpan<char> FormatParameterName(ReadOnlySpan<char> parameterName)
	{
		if (parameterName.TryLastIndexOfAndNotEnd('.', out int index))
		{
			unsafe
			{
				// SAFETY: TryLastIndexOfAndNotEnd returns true only for an index before the last character, so index + 1 is
				// within parameterName.
				parameterName = parameterName.UnsafeSlice(index + 1);
			}
		}

		return parameterName;
	}

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_BoundPositionally")]
	private static extern safe List<string> GetBoundPositionList(
		   [UnsafeAccessorType("System.Management.Automation.PSBoundParametersDictionary, System.Management.Automation")]
		   object o);
}
