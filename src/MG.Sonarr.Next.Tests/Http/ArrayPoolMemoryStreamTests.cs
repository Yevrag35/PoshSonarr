using MG.Sonarr.Next.Services.Http.IO;

namespace MG.Sonarr.Next.Tests.Http;

/// <summary>
/// Tests the pooled stream that the debug, error, and testing handlers move request and response bodies through.
/// </summary>
public sealed class ArrayPoolMemoryStreamTests
{
	/// <summary>
	/// The bytes that a write takes, in hexadecimal: up to 300 of them, more than the 16 that the stream starts with.
	/// </summary>
	private static readonly Gen<string> s_bytes = Gen.Byte.Array[0, 300].Select(bytes => Convert.ToHexString(bytes));

	/// <summary>
	/// The changes and reads that the property makes, which cover every member that moves the stream's contents or its
	/// position, with offsets, positions, and lengths past the end of the contents and, for the ones that turn them
	/// away, before the start.
	/// </summary>
	private static readonly Gen<StreamChange> s_changes = Gen.OneOf<StreamChange>(
		Gen.Select(s_bytes, Gen.Int[0, 300], Gen.Int[0, 300], (hex, offset, count) => (StreamChange)new WriteArray(hex, offset, count)),
		s_bytes.Select(hex => (StreamChange)new WriteSpan(hex)),
		s_bytes.Select(hex => (StreamChange)new WriteMemoryAsync(hex)),
		Gen.Byte.Select(value => (StreamChange)new WriteOne(value)),
		Gen.Int[0, 300].Select(count => (StreamChange)new ReadArray(count)),
		Gen.Int[0, 300].Select(count => (StreamChange)new ReadSpan(count)),
		Gen.Int[0, 300].Select(count => (StreamChange)new ReadMemoryAsync(count)),
		Gen.Const<StreamChange>(new ReadOne()),
		Gen.Select(Gen.Long[-200, 20_000], Gen.Enum<SeekOrigin>(), (offset, origin) => (StreamChange)new SeekTo(offset, origin)),
		Gen.Long[-5, 20_000].Select(position => (StreamChange)new SetPosition(position)),
		Gen.Long[-5, 20_000].Select(length => (StreamChange)new SetLength(length)),
		Gen.Const<StreamChange>(new Rewind()));

	/// <summary>
	/// An <see cref="ArrayPoolMemoryStream"/> holds the same bytes and position as a <see cref="MemoryStream"/> that the
	/// same writes, reads, seeks, and length changes are made to, and each read returns the same bytes, through every
	/// growth into a larger rented array. The bytes between the old end and a new one, which a write past the end or a
	/// longer <see cref="Stream.SetLength(long)"/> exposes, read as zeros, as <see cref="MemoryStream"/>'s do, even from a
	/// pool whose arrays hold bytes from earlier use, and a seek, position, or length that the stream turns away throws
	/// what <see cref="MemoryStream"/> throws for it.
	/// </summary>
	[Fact]
	public void HoldsTheBytesThatAMemoryStreamHoldsAfterTheSameChanges()
	{
		s_changes.Array[0, 40].Sample(changes =>
		{
			using ArrayPoolMemoryStream stream = new(initialCapacity: 0, DirtyPool.Instance);
			using MemoryStream model = new();

			foreach (StreamChange change in changes)
			{
				Apply(stream, model, change);
				AssertHolds(stream, model);
			}
		}, iter: 3_000, print: changes => string.Join(", ", changes));
	}

	/// <summary>
	/// Makes a change to a stream and to the <see cref="MemoryStream"/> that models it, and asserts that each gives the
	/// same result, or throws the same exception.
	/// </summary>
	private static void Apply(ArrayPoolMemoryStream stream, MemoryStream model, StreamChange change)
	{
		switch (change)
		{
			case WriteArray(string hex, int offset, int count):
				byte[] array = Convert.FromHexString(hex);
				int writeStart = offset % (array.Length + 1);
				int writeCount = count % (array.Length - writeStart + 1);
				stream.Write(array, writeStart, writeCount);
				model.Write(array, writeStart, writeCount);
				break;

			case WriteSpan(string hex):
				stream.Write(Convert.FromHexString(hex).AsSpan());
				model.Write(Convert.FromHexString(hex).AsSpan());
				break;

			case WriteMemoryAsync(string hex):
				stream.WriteAsync(Convert.FromHexString(hex).AsMemory(), TestContext.Current.CancellationToken).AsTask().GetAwaiter().GetResult();
				model.Write(Convert.FromHexString(hex).AsSpan());
				break;

			case WriteOne(byte value):
				stream.WriteByte(value);
				model.WriteByte(value);
				break;

			case ReadArray(int count):
				AssertReadsTheSame(count, buffer => stream.Read(buffer, 0, buffer.Length), buffer => model.Read(buffer, 0, buffer.Length));
				break;

			case ReadSpan(int count):
				AssertReadsTheSame(count, buffer => stream.Read(buffer.AsSpan()), buffer => model.Read(buffer.AsSpan()));
				break;

			case ReadMemoryAsync(int count):
				AssertReadsTheSame(
					count,
					buffer => stream.ReadAsync(buffer.AsMemory(), TestContext.Current.CancellationToken).AsTask().GetAwaiter().GetResult(),
					buffer => model.Read(buffer.AsSpan()));
				break;

			case ReadOne:
				Assert.Equal(model.ReadByte(), stream.ReadByte());
				break;

			case SeekTo(long offset, SeekOrigin origin):
				AssertSameOutcome(() => stream.Seek(offset, origin), () => model.Seek(offset, origin));
				break;

			case SetPosition(long position):
				AssertSameOutcome(() => stream.Position = position, () => model.Position = position);
				break;

			case SetLength(long length):
				AssertSameOutcome(() => { stream.SetLength(length); return length; }, () => { model.SetLength(length); return length; });
				break;

			case Rewind:
				stream.Rewind();
				model.Position = 0;
				break;

			default:
				throw new UnreachableException($"No case for {change}.");
		}
	}

	/// <summary>
	/// Asserts that a read of up to the specified number of bytes reads the same bytes from both streams.
	/// </summary>
	private static void AssertReadsTheSame(int count, Func<byte[], int> read, Func<byte[], int> readModel)
	{
		byte[] buffer = new byte[count];
		byte[] modelBuffer = new byte[count];

		int bytesRead = read(buffer);
		Assert.Equal(readModel(modelBuffer), bytesRead);
		Assert.Equal(Convert.ToHexString(modelBuffer, 0, bytesRead), Convert.ToHexString(buffer, 0, bytesRead));
	}

	/// <summary>
	/// Asserts that an action on the stream gives what the same action on the model gives, or throws the same type of
	/// exception.
	/// </summary>
	private static void AssertSameOutcome(Func<long> act, Func<long> actOnModel)
	{
		long? expected = null;
		long? actual = null;
		Exception? modelException = Record.Exception(() => expected = actOnModel());
		Exception? exception = Record.Exception(() => actual = act());

		Assert.True(
			exception?.GetType() == modelException?.GetType(),
			$"The stream threw {exception?.GetType().Name ?? "nothing"}, where a MemoryStream throws {modelException?.GetType().Name ?? "nothing"}.");
		Assert.Equal(expected, actual);
	}

	/// <summary>
	/// Asserts that a stream holds the bytes and position that its model holds.
	/// </summary>
	private static void AssertHolds(ArrayPoolMemoryStream stream, MemoryStream model)
	{
		string expected = Convert.ToHexString(model.ToArray());

		Assert.Equal(model.Length, stream.Length);
		Assert.Equal(model.Position, stream.Position);
		Assert.Equal(expected, Convert.ToHexString(stream.ToArray()));
		Assert.Equal(expected, Convert.ToHexString(stream.AsSpan()));
		Assert.Equal(expected, Convert.ToHexString(stream.AsMemory().Span));
		Assert.True(stream.TryGetBuffer(out var slice), "A stream that isn't disposed gave no buffer.");
		Assert.Equal(expected, Convert.ToHexString(slice.AsSpan()));
	}

	/// <summary>
	/// A pool that rents new arrays full of a byte that isn't zero, as a pool's arrays are after earlier use, and keeps
	/// nothing that's returned to it.
	/// </summary>
	private sealed class DirtyPool : ArrayPool<byte>
	{
		/// <summary>
		/// The pool that every stream of the property rents from.
		/// </summary>
		internal static readonly DirtyPool Instance = new();

		public override byte[] Rent(int minimumLength)
		{
			byte[] array = new byte[minimumLength];
			Array.Fill(array, (byte)0xDD);
			return array;
		}

		public override void Return(byte[] array, bool clearArray = false)
		{
		}
	}

	/// <summary>
	/// A write, read, seek, or length change that the property makes to a stream.
	/// </summary>
	private abstract record StreamChange;

	private sealed record WriteArray(string Hex, int Offset, int Count) : StreamChange;

	private sealed record WriteSpan(string Hex) : StreamChange;

	private sealed record WriteMemoryAsync(string Hex) : StreamChange;

	private sealed record WriteOne(byte Value) : StreamChange;

	private sealed record ReadArray(int Count) : StreamChange;

	private sealed record ReadSpan(int Count) : StreamChange;

	private sealed record ReadMemoryAsync(int Count) : StreamChange;

	private sealed record ReadOne : StreamChange;

	private sealed record SeekTo(long Offset, SeekOrigin Origin) : StreamChange;

	private sealed record SetPosition(long Position) : StreamChange;

	private sealed record SetLength(long Length) : StreamChange;

	private sealed record Rewind : StreamChange;
}
