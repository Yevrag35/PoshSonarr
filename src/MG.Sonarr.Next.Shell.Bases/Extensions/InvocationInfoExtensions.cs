using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Shell.Extensions;

public static class InvocationInfoExtensions
{
	[DebuggerStepThrough]
	public static bool IsBoundPositionally<T>(this InvocationInfo invocation, T value, [CallerArgumentExpression(nameof(value))] string parameterName = "") where T : struct
	{
		return IsBoundPositionally(invocation, parameterName);
	}
	public static bool IsBoundPositionally(this InvocationInfo invocation, string parameterName)
	{
		var bound = GetBoundPositionList(invocation.BoundParameters);
		return bound.Contains(parameterName);
	}

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_BoundPositionally")]
	private static extern List<string> GetBoundPositionList(
		   [UnsafeAccessorType("System.Management.Automation.PSBoundParametersDictionary, System.Management.Automation")]
		   object o);
}
