namespace MG.Sonarr.Next.Buffers;

public readonly partial struct NIndex
{
	public int CompareTo(NIndex other) => IntHelper.Compare(_data, other._data);
	public int CompareTo(int other) => IntHelper.Compare(_data, other);
	public int CompareTo(uint other)
	{
		switch (_data)
		{
			case -1:
				goto default;

			case >= 0 when other <= int.MaxValue:
				return ((uint)_data).CompareTo(other);

			default:
				return -1;
		}
	}

	public static bool operator ==(NIndex left, NIndex right) => left._data == right._data;
	public static bool operator !=(NIndex left, NIndex right) => left._data != right._data;
	public static bool operator >(NIndex left, NIndex right) => left._data > right._data;
	public static bool operator <(NIndex left, NIndex right) => left._data < right._data;
	public static bool operator >=(NIndex left, NIndex right) => left._data >= right._data;
	public static bool operator <=(NIndex left, NIndex right) => left._data <= right._data;


	public static bool operator ==(NIndex left, uint right) => left.IsValid && (uint)left._data == right;
	public static bool operator !=(NIndex left, uint right) => left.IsInvalid || (uint)left._data != right;
	public static bool operator >(NIndex left, uint right) => left.IsValid && (uint)left._data > right;
	public static bool operator <(NIndex left, uint right) => left.IsInvalid || (uint)left._data < right;
	public static bool operator >=(NIndex left, uint right) => left.IsValid && (uint)left._data >= right;
	public static bool operator <=(NIndex left, uint right) => left.IsInvalid || (uint)left._data <= right;
	public static bool operator >(uint left, NIndex right) => right.IsInvalid || left > (uint)right._data;
	public static bool operator <(uint left, NIndex right) => right.IsValid && left < (uint)right._data;
	public static bool operator >=(uint left, NIndex right) => right.IsInvalid || left >= (uint)right._data;
	public static bool operator <=(uint left, NIndex right) => right.IsValid && left <= (uint)right._data;

	public static bool operator ==(NIndex left, int right) => left._data == right;
	public static bool operator !=(NIndex left, int right) => left._data != right;
	public static bool operator >(NIndex left, int right) => left._data > right;
	public static bool operator <(NIndex left, int right) => left._data < right;
	public static bool operator >=(NIndex left, int right) => left._data >= right;
	public static bool operator <=(NIndex left, int right) => left._data <= right;
	public static bool operator >(int left, NIndex right) => left > right._data;
	public static bool operator <(int left, NIndex right) => left < right._data;
	public static bool operator >=(int left, NIndex right) => left >= right._data;
	public static bool operator <=(int left, NIndex right) => left <= right._data;

	public static bool operator ==(NIndex left, byte right) => left._data == right;
	public static bool operator !=(NIndex left, byte right) => left._data != right;
	public static bool operator ==(byte left, NIndex right) => left == right._data;
	public static bool operator !=(byte left, NIndex right) => left != right._data;
	public static bool operator >(NIndex left, byte right) => left._data > right;
	public static bool operator <(NIndex left, byte right) => left._data < right;
	public static bool operator >=(NIndex left, byte right) => left._data >= right;
	public static bool operator <=(NIndex left, byte right) => left._data <= right;
	public static bool operator >(byte left, NIndex right) => left > right._data;
	public static bool operator <(byte left, NIndex right) => left < right._data;
	public static bool operator >=(byte left, NIndex right) => left >= right._data;
	public static bool operator <=(byte left, NIndex right) => left <= right._data;

	public static bool operator >=(NIndex left, ushort right)
	{
		return left._data >= right;
	}
	public static bool operator <=(NIndex left, ushort right)
	{
		return left._data <= right;
	}
}