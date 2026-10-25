namespace MG.Sonarr.Next.Buffers;

public readonly partial struct NIndex
{
	public bool Equals(NIndex other)
	{
		return this == other;
	}
	public bool Equals(int other)
	{
		return this == other;
	}
	public bool Equals(uint other)
	{
		return this == other;
	}
	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return obj switch
		{
			null => false,
			NIndex index => this == index,
			int intVal when intVal >= -1 => this == intVal,
			uint uintVal when uintVal <= int.MaxValue => this == uintVal,
			byte b when this.IsValid => _data == b,
			long longVal when longVal is >= -1 and <= int.MaxValue => this == (int)longVal,
			ulong ulongVal when ulongVal <= int.MaxValue => this == (int)ulongVal,
			_ => false,
		};
	}
	public override int GetHashCode()
	{
		return _data;
	}
}