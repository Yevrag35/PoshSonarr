using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Resources;

namespace MG.Sonarr.Next.Guarding;

public static partial class ThrowHelper
{
	/// <exception cref="ArgumentException"/>
	[DoesNotReturn]
	[DebuggerStepThrough]
	internal static void ThrowBadType<T>(string? paramName, Type? receivedType)
	{
		ThrowBadType<T>(paramName, receivedType, acceptedDerivatives: true);
	}
	/// <exception cref="ArgumentException"/>
	[DoesNotReturn]
	internal static void ThrowBadType<T>(string? paramName, Type? receivedType, bool acceptedDerivatives)
	{
		string msgFormat = acceptedDerivatives
			? ExMessages.Reflection_ExpectedTypeOrDerivative
			: ExMessages.Reflection_ExpectedExactlyType;

		string expectedTypeName = typeof(T).GetName();
		string receivedTypeName = receivedType.GetNameOrNull() ?? ExMessages.Null_AsString;

		string castMessage = string.Format(
			provider: CultureInfo.CurrentCulture,
			format: ExMessages.Reflection_CannotCastToType,
			arg0: expectedTypeName);

		string argMessage = string.Format(
			provider: CultureInfo.CurrentCulture,
			format: msgFormat,
			arg0: expectedTypeName,
			arg1: receivedTypeName);

		InvalidCastException castEx = new(castMessage);
		throw new ArgumentException(argMessage, paramName, castEx);
	}
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	/// <param name="paramName"></param>
	/// <param name="receivedType"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	[DebuggerStepThrough]
	[DoesNotReturn]
	internal static void ThrowNeitherType<T1, T2>(string? paramName, Type? receivedType)
	{
		string expectedType1 = typeof(T1).GetName();
		string expectedType2 = typeof(T2).GetName();
		string receivedTypeName = receivedType.GetNameOr(ExMessages.Null_AsString);
		string argMessage = string.Format(
			provider: CultureInfo.CurrentCulture,
			format: ExMessages.Reflection_ExpectedEitherType_2,
			arg0: expectedType1,
			arg1: expectedType2,
			arg2: receivedTypeName);

		throw new ArgumentException(argMessage, paramName);
	}
}
