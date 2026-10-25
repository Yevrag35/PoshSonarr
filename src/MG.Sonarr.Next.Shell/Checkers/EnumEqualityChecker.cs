using System.Numerics;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Shell.Checkers;

public sealed class EnumEqualityChecker<T> : EqualityChecker where T : unmanaged, Enum
{
	private readonly Type _type;
	private readonly Type _underlyingType;

	public EnumEqualityChecker()
	{
		_type = typeof(T);
		_underlyingType = Enum.GetUnderlyingType(_type);
	}

	protected override bool EqualsCore([DisallowNull] object x, [DisallowNull] object y)
	{
		if (!LanguagePrimitives.TryConvertTo(x, out T xValue)
			||
			!LanguagePrimitives.TryConvertTo(y, out T yValue))
		{
			return false;
		}

		return this.AreValuesEqual(ref xValue, ref yValue);
	}

	private bool AreValuesEqual(ref T x, ref T y)
	{
		if (_underlyingType.Equals(typeof(int)))
		{
			return AreValuesEqual<int>(ref x, ref y);
		}
		else if (_underlyingType.Equals(typeof(long)))
		{
			return AreValuesEqual<long>(ref x, ref y);
		}
		else if (_underlyingType.Equals(typeof(byte)))
		{
			return AreValuesEqual<byte>(ref x, ref y);
		}
		else
		{
			return x.Equals(y);
		}
	}

	private static bool AreValuesEqual<TNum>(ref T x, ref T y) where TNum : unmanaged, INumber<TNum>
	{
		ref readonly TNum xAsNum = ref Unsafe.As<T, TNum>(ref x);
		ref readonly TNum yAsNum = ref Unsafe.As<T, TNum>(ref y);

		return xAsNum.Equals(yAsNum);
	}

	protected override int GetHashCodeCore([DisallowNull] object obj)
	{
		return obj.GetHashCode();
	}
}
