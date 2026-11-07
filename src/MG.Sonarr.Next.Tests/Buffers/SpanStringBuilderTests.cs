using MG.Sonarr.Next.Buffers;

namespace MG.Sonarr.Next.Tests.Buffers;

/// <summary>
/// Tests the builder that request URLs and wildcard patterns are built with, which writes into a span on the stack and
/// grows into arrays that it rents.
/// </summary>
public sealed class SpanStringBuilderTests
{
	/// <summary>
	/// The characters that the changes write: ASCII and non-ASCII letters, white space, and the null character, which
	/// some members write by clearing rather than filling.
	/// </summary>
	private static readonly Gen<char> s_chars = Gen.OneOf('a', 'b', 'é', ' ', '\n', '\0');

	/// <summary>
	/// The characters that a callback writes through a function pointer that returns how many it wrote: those of
	/// <see cref="s_chars"/> but the null character, since a callback of that overload writes none, as a Debug build
	/// asserts.
	/// </summary>
	private static readonly Gen<char> s_charsButNull = Gen.OneOf('a', 'b', 'é', ' ', '\n');

	/// <summary>
	/// The text that the changes write: none, short text, text outside ASCII, and text longer than any span that a
	/// builder starts on.
	/// </summary>
	private static readonly Gen<string> s_text = Gen.OneOf("", "x", "hello", "日本語", new string('w', 300));

	/// <summary>
	/// The values that the builder formats, each with a format that its type takes.
	/// </summary>
	private static readonly Gen<(ISpanFormattable Value, string? Format)> s_formattables = Gen.OneOf(
		Gen.Select(Gen.Int, Gen.OneOf<string?>(null, "D", "X", "N0"), (value, format) => ((ISpanFormattable)value, format)),
		Gen.Select(Gen.Long, Gen.OneOf<string?>(null, "D", "N0"), (value, format) => ((ISpanFormattable)value, format)),
		Gen.Select(Gen.Double, Gen.OneOf<string?>(null, "R", "E3", "F2"), (value, format) => ((ISpanFormattable)value, format)),
		Gen.Select(Gen.DateTime, Gen.OneOf<string?>(null, "O", "yyyy-MM-ddTHH:mm:ss"), (value, format) => ((ISpanFormattable)value, format)),
		Gen.Select(Gen.Guid, Gen.OneOf<string?>(null, "N", "B"), (value, format) => ((ISpanFormattable)value, format)));

	/// <summary>
	/// The ways that a builder starts: on a span of up to 256 characters, in an array rented for a capacity of up to 300,
	/// or from <see langword="default"/>, through each member that starts one.
	/// </summary>
	private static readonly Gen<Start> s_starts = Gen.Frequency<Start>(
		(4, Gen.Select(Gen.Int[0, 256], Gen.Enum<SpanStart>(), (length, how) => (Start)new OnSpan(length, how))),
		(4, Gen.Select(Gen.Int[0, 300], Gen.Bool, (capacity, throughCreate) => (Start)new Rented(capacity, throughCreate))),
		(1, Gen.Const<Start>(new FromDefault())));

	/// <summary>
	/// The changes that the properties make to a builder, which cover every public member that writes to it.
	/// </summary>
	private static readonly Gen<Change> s_changes = Gen.OneOf<Change>(
		s_chars.Select(value => (Change)new AppendChar(value)),
		Gen.Select(s_chars, Gen.Int[0, 300], (value, count) => (Change)new AppendRepeated(value, count)),
		s_text.Select(value => (Change)new AppendText(value)),
		Gen.Select(s_chars, s_chars, (first, second) => (Change)new AppendCharacters(first, second)),
		Gen.Select(s_formattables, Gen.Int[1, 80], Gen.OneOf<string?>(null, "", "fr-FR", "th-TH"),
			(formattable, maxLength, culture) => (Change)new AppendFormatted(formattable.Value, maxLength, formattable.Format, culture)),
		Gen.Select(Gen.Guid, Gen.OneOf("", "N", "D", "B", "P", "X", "n", "d", "b", "p", "x"), (value, format) => (Change)new AppendGuidText(value, format)),
		Gen.Int[0, 0x7FFFF].Select(value => (Change)new AppendEnum((FileAttributes)value)),
		Gen.Byte.Array[0, 40].Select(bytes => (Change)new AppendUtf8(Convert.ToHexString(bytes))),
		s_text.Select(value => (Change)new AppendUtf8(Convert.ToHexString(Encoding.UTF8.GetBytes(value)))),
		Gen.Const<Change>(new AppendNewLine()),
		s_text.Select(value => (Change)new AppendLineText(value)),
		Gen.Select(s_text, s_chars, (value, enclosure) => (Change)new AppendEnclosed(value, enclosure)),
		Gen.Select(Gen.Int[0, 300], s_chars, (length, value) => (Change)new AppendFilled(length, value)),
		Gen.Select(Gen.Int[0, 300], s_chars, (length, value) => (Change)new AppendWritten(length, value)),
		Gen.Select(Gen.Int[0, 300], s_chars, (length, value) => (Change)new AppendWrittenThroughPointer(length, value)),
		Gen.Select(Gen.Int[0, 100], Gen.Int[-2, 102], s_chars, (maxLength, written, value) => (Change)new AppendWrittenUpTo(maxLength, written, value)),
		Gen.Select(Gen.Int[0, 100], Gen.Int[-2, 102], s_charsButNull, (maxLength, written, value) => (Change)new AppendWrittenUpToThroughPointer(maxLength, written, value)),
		Gen.Select(Gen.Int[0, 1000], s_chars, (index, value) => (Change)new InsertChar(index, value)),
		Gen.Select(Gen.Int[0, 1000], s_chars, Gen.Int[0, 300], (index, value, count) => (Change)new InsertRepeated(index, value, count)),
		Gen.Select(Gen.Int[0, 1000], s_text, (index, value) => (Change)new InsertText(index, value)),
		Gen.Select(Gen.Int[0, 1000], s_text, (index, value) => (Change)new Overwrite(index, value)),
		Gen.Select(Gen.Int[0, 1000], Gen.Int[0, 300], s_chars, (index, length, value) => (Change)new OverwriteFilled(index, length, value)),
		Gen.Select(Gen.Int[0, 1000], Gen.Int[0, 1000], (start, length) => (Change)new RemoveRange(start, length)),
		Gen.Select(Gen.Int[0, 1000], s_chars, (index, value) => (Change)new SetChar(index, value)),
		Gen.Const<Change>(new RemoveLast()),
		s_chars.Select(value => (Change)new RemoveLastIf(value)),
		s_chars.Select(value => (Change)new TrimTrailing(value)),
		Gen.Const<Change>(new ClearAll()));

	/// <summary>
	/// How far outside its range an argument falls: below it when negative and above it when positive, by one to three
	/// or as far as an <see cref="int"/> goes.
	/// </summary>
	private static readonly Gen<int> s_offsets = Gen.OneOf(int.MinValue, -3, -2, -1, 1, 2, 3, int.MaxValue);

	/// <summary>
	/// The values of a count or a length below its range: just below 0, or as far below as an <see cref="int"/> goes.
	/// </summary>
	private static readonly Gen<int> s_negatives = Gen.OneOf(int.MinValue, -3, -2, -1);

	/// <summary>
	/// The calls that the rejection property makes, which cover every check of an argument that the docs of the
	/// builder's public members name.
	/// </summary>
	private static readonly Gen<Rejection> s_rejections = Gen.OneOf<Rejection>(
		Gen.Select(s_offsets, Gen.Bool, (offset, write) => (Rejection)new CharAt(offset, write)),
		s_negatives.Select(count => (Rejection)new NegativeCopies(count)),
		Gen.OneOf(int.MinValue, -1, 0).Select(maxLength => (Rejection)new NoRoomToFormat(maxLength)),
		Gen.Select(Gen.Enum<Writer>(), s_negatives, (writer, length) => (Rejection)new NegativeRoomToWrite(writer, length)),
		Gen.Select(Gen.Bool, Gen.Int[0, 3], (countsWritten, length) => (Rejection)new NullWriter(countsWritten, length)),
		Gen.OneOf("Q", "G", "z", "0", " ", "é", "DD", "Nn", "D ", "BPX").Select(format => (Rejection)new InvalidGuidFormat(format)),
		Gen.Select(s_offsets, Gen.Bool, (offset, counted) => (Rejection)new SearchFrom(offset, counted)),
		Gen.Select(Gen.Int[0, 1000], s_offsets, (at, offset) => (Rejection)new SearchCounting(at, offset)),
		s_offsets.Select(offset => (Rejection)new InsertCharAt(offset)),
		Gen.Select(s_offsets, Gen.Int[0, 3], (offset, count) => (Rejection)new InsertCopiesAt(offset, count)),
		Gen.Select(s_offsets, s_text, (offset, value) => (Rejection)new InsertTextAt(offset, value)),
		Gen.Select(Gen.Int[0, 1000], s_negatives, (at, count) => (Rejection)new NegativeInsertedCopies(at, count)),
		Gen.Select(s_offsets, s_text, (offset, value) => (Rejection)new OverwriteAt(offset, value)),
		Gen.Select(s_offsets, Gen.Int[0, 3], (offset, length) => (Rejection)new OverwriteSpanAt(offset, length)),
		Gen.Select(Gen.Int[0, 1000], s_negatives, (at, length) => (Rejection)new NegativeOverwriteSpan(at, length)),
		s_offsets.Select(offset => (Rejection)new RemoveFrom(offset)),
		Gen.Select(Gen.Int[0, 1000], s_offsets, (at, offset) => (Rejection)new RemoveCounting(at, offset)),
		Gen.Int[1, 3].Select(by => (Rejection)new ShortDestination(by)),
		s_offsets.Select(offset => (Rejection)new SliceFrom(offset)),
		Gen.Select(Gen.Int[0, 1000], s_offsets, (at, offset) => (Rejection)new SliceCounting(at, offset)),
		s_negatives.Select(capacity => (Rejection)new NegativeCapacity(capacity)));

	/// <summary>
	/// The characters that <see cref="SpanStringBuilder.ContainsAny(SearchValues{char})"/> looks for.
	/// </summary>
	private static readonly SearchValues<char> s_searchValues = SearchValues.Create("é\n");

	/// <summary>
	/// A <see cref="SpanStringBuilder"/> holds the same text as a <see cref="StringBuilder"/> that the same changes are
	/// made to, however it starts and through every growth into a larger rented array. One that starts on the caller's
	/// span writes into that span and rents nothing until it outgrows it, a change that asks for no more room than it
	/// writes grows the buffer only when the text no longer fits, and the buffer never shrinks. A value that doesn't fit
	/// in the length that its caller allows throws a <see cref="FormatException"/> and leaves the text as it was. Each
	/// search finds what it finds in the text, and
	/// <see cref="SpanStringBuilder.EndsWith(ReadOnlySpan{char}, StringComparison)"/> is <see langword="false"/> for an
	/// empty builder, as its docs say. Building the string returns the builder to its default state, with nothing
	/// rented, so that disposing it again returns nothing more to the pool.
	/// </summary>
	[Fact]
	public void HoldsTheTextThatAStringBuilderHoldsAfterTheSameChanges()
	{
		Gen.Select(s_starts, s_changes.Array[0, 30], s_chars).Sample((start, changes, probe) =>
		{
			Span<char> stack = stackalloc char[start is OnSpan onSpan ? onSpan.Length : 0];
			SpanStringBuilder builder = Begin(start, stack);
			AssertStarted(ref builder, start, stack.Length);

			int startCapacity = builder.Capacity;
			StringBuilder model = new();
			foreach (Change change in changes)
			{
				int capacityBefore = builder.Capacity;
				Apply(ref builder, model, change);

				string text = model.ToString();
				AssertHolds(ref builder, text, probe);
				AssertGrowth(ref builder, change, capacityBefore, text.Length);

				// A builder rents exactly when it first grows past the buffer that it started with, and until then it
				// holds its text in the caller's span.
				Assert.Equal(start is Rented || builder.Capacity > startCapacity, builder.IsRented);
				if (!builder.IsRented)
				{
					Assert.Equal(text, stack[..text.Length].ToString());
				}
			}

			Assert.Equal(model.ToString(), builder.Build());
			AssertDefault(ref builder);
			builder.Dispose();
			AssertDefault(ref builder);
		}, iter: 2_000, print: t => string.Create(
			CultureInfo.InvariantCulture,
			$"starting {t.Item1}, with {string.Join(", ", t.Item2)}, probing for U+{(int)t.Item3:X4}"));
	}

	/// <summary>
	/// A member that checks an argument, given one outside the range that it takes, throws the exception that its docs
	/// name and leaves the builder's text and buffer as they were, whatever text the builder holds and however it
	/// started. Each argument falls just outside its range, at either end, or as far outside as an <see cref="int"/>
	/// goes, where a check that adds two arguments would overflow. Most members reach into the buffer through references
	/// and slices that nothing checks, so these checks are all that keeps a bad argument from reading past the text or
	/// writing outside the buffer.
	/// </summary>
	[Fact]
	public void RejectsAnArgumentOutsideItsRangeAndLeavesTheBuilderAsItWas()
	{
		Gen.Select(s_starts, s_changes.Array[0, 12], s_rejections.Array[1, 8]).Sample((start, changes, rejections) =>
		{
			Span<char> stack = stackalloc char[start is OnSpan onSpan ? onSpan.Length : 0];
			SpanStringBuilder builder = Begin(start, stack);
			StringBuilder model = new();
			foreach (Change change in changes)
			{
				Apply(ref builder, model, change);
			}

			string text = builder.ToString();
			int capacity = builder.Capacity;
			bool rented = builder.IsRented;
			foreach (Rejection rejection in rejections)
			{
				Type? expected = Expected(rejection, text.Length);
				if (expected is null)
				{
					continue;
				}

				Exception? thrown = null;
				try
				{
					Reject(ref builder, rejection);
				}
				catch (ArgumentException e)
				{
					thrown = e;
				}
				catch (FormatException e)
				{
					thrown = e;
				}

				Assert.True(thrown is not null, $"{rejection} threw nothing.");
				Assert.IsType(expected, thrown);
				Assert.Equal(text, builder.ToString());
				Assert.Equal(capacity, builder.Capacity);
				Assert.Equal(rented, builder.IsRented);
			}

			builder.Dispose();
		}, iter: 1_000, print: t => string.Create(
			CultureInfo.InvariantCulture,
			$"starting {t.Item1}, with {string.Join(", ", t.Item2)}, then {string.Join(", ", t.Item3)}"));
	}

	/// <summary>
	/// Starts a builder the way that a start says, on a span when it starts on one.
	/// </summary>
	/// <param name="start">How the builder starts.</param>
	/// <param name="stack">The span that the builder starts on when <paramref name="start"/> is an <see cref="OnSpan"/>.</param>
	/// <returns>The builder, which the caller disposes.</returns>
	private static SpanStringBuilder Begin(Start start, Span<char> stack)
	{
		return start switch
		{
			OnSpan { How: SpanStart.Constructor } => new SpanStringBuilder(stack),
			OnSpan { How: SpanStart.Create } => SpanStringBuilder.Create(stack),
			OnSpan => stack,
			Rented { ThroughCreate: true } rented => SpanStringBuilder.Create(rented.Capacity),
			Rented rented => new SpanStringBuilder(rented.Capacity),
			_ => default,
		};
	}

	/// <summary>
	/// Makes a change to a builder and to the <see cref="StringBuilder"/> that models it.
	/// </summary>
	/// <param name="builder">The builder to change.</param>
	/// <param name="model">The model of the builder's text.</param>
	/// <param name="change">The change to make.</param>
	private static void Apply(ref SpanStringBuilder builder, StringBuilder model, Change change)
	{
		int length = model.Length;
		switch (change)
		{
			case AppendChar(char value):
				builder.Append(value);
				_ = model.Append(value);
				break;

			case AppendRepeated(char value, int count):
				builder.Append(value, count);
				_ = model.Append(value, count);
				break;

			case AppendText(string value):
				builder.Append(value);
				_ = model.Append(value);
				break;

			case AppendCharacters(char first, char second):
				builder.AppendChars(first, second);
				_ = model.Append(first).Append(second);
				break;

			case AppendFormatted(ISpanFormattable value, int maxLength, var format, var culture):
				IFormatProvider? provider = culture is null ? null : CultureInfo.GetCultureInfo(culture);
				string formatted = value.ToString(format, provider);
				if (formatted.Length > maxLength)
				{
					SpanStringBuilder before = builder;
					FormatException? thrown = null;
					try
					{
						builder.Append(value, maxLength, format, provider);
					}
					catch (FormatException e)
					{
						thrown = e;
					}

					Assert.True(thrown is not null, $"'{formatted}' didn't fit in {maxLength} characters, and Append didn't throw.");
					Assert.Equal(before.Length, builder.Length);
				}
				else
				{
					builder.Append(value, maxLength, format, provider);
					_ = model.Append(formatted);
				}

				break;

			case AppendGuidText(Guid value, string format):
				builder.AppendGuid(value, format);
				_ = model.Append(value.ToString(format, CultureInfo.InvariantCulture));
				break;

			case AppendEnum(FileAttributes value):
				builder.Append(value);
				_ = model.Append(value.ToString());
				break;

			case AppendUtf8(string hex):
				byte[] bytes = Convert.FromHexString(hex);
				builder.Append((ReadOnlySpan<byte>)bytes);
				_ = model.Append(Encoding.UTF8.GetString(bytes));
				break;

			case AppendNewLine:
				builder.AppendLine();
				_ = model.Append(Environment.NewLine);
				break;

			case AppendLineText(string value):
				builder.AppendLine(value);
				_ = model.Append(value).Append(Environment.NewLine);
				break;

			case AppendEnclosed(string value, char enclosure):
				builder.AppendEnclosedIn(value, enclosure);
				if (value.Length > 0)
				{
					_ = model.Append(enclosure).Append(value).Append(enclosure);
				}

				break;

			case AppendFilled(int fillLength, char value):
				builder.AppendSpan(fillLength).Fill(value);
				_ = model.Append(value, fillLength);
				break;

			case AppendWritten(int writeLength, char value):
				builder.Append(writeLength, value, FillWith);
				_ = model.Append(value, writeLength);
				break;

			case AppendWrittenThroughPointer(int writeLength, char value):
				unsafe
				{
					builder.Append(writeLength, value, &FillWith);
				}

				_ = model.Append(value, writeLength);
				break;

			case AppendWrittenUpTo(int maxLength, int written, char value):
				builder.Append(maxLength, (Value: value, Written: written), FillUpTo);
				_ = model.Append(value, Math.Clamp(written, 0, maxLength));
				break;

			case AppendWrittenUpToThroughPointer(int maxLength, int written, char value):
				unsafe
				{
					builder.Append(maxLength, (Value: value, Written: written), &FillUpTo);
				}

				_ = model.Append(value, Math.Clamp(written, 0, maxLength));
				break;

			case InsertChar(int index, char value):
				builder.Insert(index % (length + 1), value);
				_ = model.Insert(index % (length + 1), value);
				break;

			case InsertRepeated(int index, char value, int count):
				builder.Insert(index % (length + 1), value, count);
				_ = model.Insert(index % (length + 1), new string(value, count));
				break;

			case InsertText(int index, string value):
				builder.Insert(index % (length + 1), value);
				_ = model.Insert(index % (length + 1), value);
				break;

			case Overwrite(int index, string value):
				builder.OverlapAppend(index % (length + 1), value);
				if (value.Length > 0)
				{
					model.Length = index % (length + 1);
					_ = model.Append(value);
				}

				break;

			case OverwriteFilled(int index, int fillLength, char value):
				builder.OverlapAppendSpan(index % (length + 1), fillLength).Fill(value);
				if (fillLength > 0)
				{
					model.Length = index % (length + 1);
					_ = model.Append(value, fillLength);
				}

				break;

			case RemoveRange(int start, int removeLength):
				int removeFrom = start % (length + 1);
				int removeCount = removeLength % (length - removeFrom + 1);
				builder.Remove(removeFrom, removeCount);
				_ = model.Remove(removeFrom, removeCount);
				break;

			case SetChar(int index, char value) when length > 0:
				builder[index % length] = value;
				model[index % length] = value;
				break;

			case SetChar:
				break;

			case RemoveLast:
				builder.Backspace();
				model.Length = Math.Max(0, length - 1);
				break;

			case RemoveLastIf(char value):
				builder.BackspaceIf(value);
				if (length > 0 && model[length - 1] == value)
				{
					model.Length = length - 1;
				}

				break;

			case TrimTrailing(char value):
				builder.TrimEnd(value);
				while (model.Length > 0 && model[model.Length - 1] == value)
				{
					model.Length--;
				}

				break;

			case ClearAll:
				builder.Clear();
				_ = model.Clear();
				break;

			default:
				throw new UnreachableException($"No case for {change}.");
		}
	}

	/// <summary>
	/// Makes a call with an argument outside the range that its member takes.
	/// </summary>
	/// <param name="builder">The builder to make the call to.</param>
	/// <param name="rejection">The call to make.</param>
	private static void Reject(ref SpanStringBuilder builder, Rejection rejection)
	{
		int length = builder.Length;
		int capacity = builder.Capacity;
		switch (rejection)
		{
			case CharAt(int offset, true):
				builder[Outside(offset, length - 1)] = 'x';
				break;

			case CharAt(int offset, false):
				_ = builder[Outside(offset, length - 1)];
				break;

			case NegativeCopies(int count):
				builder.Append('x', count);
				break;

			case NoRoomToFormat(int maxLength):
				builder.Append(42, maxLength, provider: CultureInfo.InvariantCulture);
				break;

			case NegativeRoomToWrite(Writer.Action, int writeLength):
				builder.Append(writeLength, 'x', FillWith);
				break;

			case NegativeRoomToWrite(Writer.Func, int writeLength):
				builder.Append(writeLength, (Value: 'x', Written: 0), FillUpTo);
				break;

			case NegativeRoomToWrite(Writer.ActionPointer, int writeLength):
				unsafe
				{
					builder.Append(writeLength, 'x', &FillWith);
				}

				break;

			case NegativeRoomToWrite(Writer.FuncPointer, int writeLength):
				unsafe
				{
					builder.Append(writeLength, (Value: 'x', Written: 0), &FillUpTo);
				}

				break;

			case NullWriter(false, int writeLength):
				unsafe
				{
					builder.Append(writeLength, 'x', (delegate*<Span<char>, char, void>)null);
				}

				break;

			case NullWriter(true, int writeLength):
				unsafe
				{
					builder.Append(writeLength, 'x', (delegate*<Span<char>, char, int>)null);
				}

				break;

			case InvalidGuidFormat(string format):
				builder.AppendGuid(Guid.Empty, format);
				break;

			case SearchFrom(int offset, true):
				_ = builder.IndexOf('x', Outside(offset, length), 0);
				break;

			case SearchFrom(int offset, false):
				_ = builder.IndexOf('x', Outside(offset, length));
				break;

			case SearchCounting(int at, int offset):
				_ = builder.IndexOf('x', Inside(at, length), Outside(offset, length - Inside(at, length)));
				break;

			case InsertCharAt(int offset):
				builder.Insert(Outside(offset, length), 'x');
				break;

			case InsertCopiesAt(int offset, int count):
				builder.Insert(Outside(offset, length), 'x', count);
				break;

			case InsertTextAt(int offset, string value):
				builder.Insert(Outside(offset, length), value);
				break;

			case NegativeInsertedCopies(int at, int count):
				builder.Insert(Inside(at, length), 'x', count);
				break;

			case OverwriteAt(int offset, string value):
				builder.OverlapAppend(Outside(offset, length), value);
				break;

			case OverwriteSpanAt(int offset, int spanLength):
				_ = builder.OverlapAppendSpan(Outside(offset, length), spanLength);
				break;

			case NegativeOverwriteSpan(int at, int spanLength):
				_ = builder.OverlapAppendSpan(Inside(at, length), spanLength);
				break;

			case RemoveFrom(int offset):
				builder.Remove(Outside(offset, length), 0);
				break;

			case RemoveCounting(int at, int offset):
				builder.Remove(Inside(at, length), Outside(offset, length - Inside(at, length)));
				break;

			case ShortDestination(int by):
				_ = builder.CopyTo(new char[length - Math.Min(by, length)]);
				break;

			case SliceFrom(int offset):
				_ = builder.AsSpan(Outside(offset, capacity), 0);
				break;

			case SliceCounting(int at, int offset):
				_ = builder.AsSpan(Inside(at, capacity), Outside(offset, capacity - Inside(at, capacity)));
				break;

			case NegativeCapacity(int negativeCapacity):
				_ = new SpanStringBuilder(negativeCapacity);
				break;

			default:
				throw new UnreachableException($"No case for {rejection}.");
		}
	}

	/// <summary>
	/// Gets the type of the exception that a rejected call's member names in its docs.
	/// </summary>
	/// <param name="rejection">The rejected call.</param>
	/// <param name="length">The length of the builder's text.</param>
	/// <returns>
	/// The type of the exception, or <see langword="null"/> when the call can't fall outside its range, as a copy can't
	/// go to a destination shorter than an empty text.
	/// </returns>
	private static Type? Expected(Rejection rejection, int length)
	{
		return rejection switch
		{
			ShortDestination when length == 0 => null,
			ShortDestination or InvalidGuidFormat { Format.Length: > 1 } => typeof(ArgumentException),
			InvalidGuidFormat => typeof(FormatException),
			NullWriter => typeof(ArgumentNullException),
			_ => typeof(ArgumentOutOfRangeException),
		};
	}

	/// <summary>
	/// Determines whether a change can ask a builder for more room than it writes: an append of what a callback writes
	/// into up to a length, of a value formatted into up to a length, or of UTF-8 text, which takes room for the most
	/// characters that its bytes could decode to.
	/// </summary>
	/// <param name="change">The change.</param>
	/// <returns>
	/// <see langword="true"/> if the change can ask for more room than it writes; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool AsksForMoreRoomThanItWrites(Change change)
	{
		return change is AppendFormatted or AppendUtf8 or AppendWrittenUpTo or AppendWrittenUpToThroughPointer;
	}

	/// <summary>
	/// Gets an argument outside the range from 0 through a maximum: the offset itself when it's negative or
	/// <see cref="int.MaxValue"/>, and the maximum plus the offset otherwise.
	/// </summary>
	/// <param name="offset">How far outside the range the argument falls, below it when negative.</param>
	/// <param name="max">The largest argument in the range.</param>
	/// <returns>The argument.</returns>
	private static int Outside(int offset, int max)
	{
		return offset switch
		{
			< 0 or int.MaxValue => offset,
			_ => max + offset,
		};
	}

	/// <summary>
	/// Gets an argument inside the range from 0 through a maximum.
	/// </summary>
	/// <param name="at">A value of 0 or more that the argument is taken from.</param>
	/// <param name="max">The largest argument in the range.</param>
	/// <returns>The argument.</returns>
	private static int Inside(int at, int max)
	{
		return at % (max + 1);
	}

	/// <summary>
	/// Fills a span with a character, as the callback of an append that writes a whole length.
	/// </summary>
	/// <param name="span">The span to fill.</param>
	/// <param name="value">The character to fill it with.</param>
	private static void FillWith(Span<char> span, char value)
	{
		span.Fill(value);
	}

	/// <summary>
	/// Fills as much of a span as a count says, as the callback of an append that writes up to a length.
	/// </summary>
	/// <param name="span">The span to fill.</param>
	/// <param name="state">The character to fill the span with, and the count, which can be negative or more than the
	/// span holds.</param>
	/// <returns>The count, as <paramref name="state"/> gives it.</returns>
	private static int FillUpTo(Span<char> span, (char Value, int Written) state)
	{
		span[..Math.Clamp(state.Written, 0, span.Length)].Fill(state.Value);
		return state.Written;
	}

	/// <summary>
	/// Asserts that a builder starts empty: on the caller's span, with the span's length as its capacity and nothing
	/// rented, in a rented array of at least the capacity that it was given, or with no buffer at all from
	/// <see langword="default"/>.
	/// </summary>
	/// <param name="builder">The builder that just started.</param>
	/// <param name="start">How the builder started.</param>
	/// <param name="stackLength">The length of the span that the builder started on, or 0 when it didn't start on one.</param>
	private static void AssertStarted(ref SpanStringBuilder builder, Start start, int stackLength)
	{
		Assert.Equal(0, builder.Length);
		Assert.Equal(string.Empty, builder.ToString());
		if (start is Rented(int capacity, _))
		{
			Assert.True(builder.IsRented, "A builder that was given a capacity didn't rent an array.");
			Assert.True(builder.Capacity >= capacity, $"A builder that was given a capacity of {capacity} has one of {builder.Capacity}.");
		}
		else
		{
			Assert.False(builder.IsRented, "A builder that started on the caller's span rented an array.");
			Assert.Equal(stackLength, builder.Capacity);
		}

		Assert.Equal(builder.Capacity == 0, builder.IsDefaultOrEmpty);
		if (start is FromDefault)
		{
			Assert.True(builder.IsDefault, "A builder that started from default doesn't say that it's default.");
		}
	}

	/// <summary>
	/// Asserts that a builder holds the specified text, and that each of its searches finds in it what the same search
	/// finds in the text.
	/// </summary>
	/// <param name="builder">The builder to check.</param>
	/// <param name="text">The text that the builder should hold.</param>
	/// <param name="probe">The character that the searches look for.</param>
	private static void AssertHolds(ref SpanStringBuilder builder, string text, char probe)
	{
		Assert.Equal(text, builder.ToString());
		Assert.Equal(text.Length, builder.Length);
		Assert.True(builder.Capacity >= builder.Length, "The builder holds more characters than its capacity.");
		Assert.Equal(text, builder.AsSpan().ToString());

		int middle = text.Length / 2;
		Assert.Equal(text.AsSpan(middle / 2, middle).ToString(), builder.AsSpan(middle / 2, middle).ToString());

		char[] copy = new char[text.Length];
		Assert.Equal(text.Length, builder.CopyTo(copy));
		Assert.Equal(text, new string(copy));

		Assert.Equal(text.IndexOf(probe), builder.IndexOf(probe));
		Assert.Equal(text.IndexOf(probe, middle), builder.IndexOf(probe, middle));
		Assert.Equal(text.IndexOf(probe, middle, text.Length - middle), builder.IndexOf(probe, middle, text.Length - middle));
		Assert.Equal(text.AsSpan().ContainsAny(s_searchValues), builder.ContainsAny(s_searchValues));

		string tail = text[middle..];
		AssertEndsWith(ref builder, text, tail, StringComparison.Ordinal);
		AssertEndsWith(ref builder, text, tail.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase);
		AssertEndsWith(ref builder, text, new string(probe, 1), StringComparison.Ordinal);
		AssertEndsWith(ref builder, text, string.Empty, StringComparison.Ordinal);
		AssertEndsWith(ref builder, text, text + probe, StringComparison.Ordinal);

		if (text.Length > 0)
		{
			Assert.Equal(text[middle], builder[middle]);
		}
	}

	/// <summary>
	/// Asserts that a builder ends with a value exactly when its text does, except that an empty builder ends with
	/// nothing, as its docs say.
	/// </summary>
	/// <param name="builder">The builder to check.</param>
	/// <param name="text">The text that the builder holds.</param>
	/// <param name="value">The value to look for at the end.</param>
	/// <param name="comparison">How to compare the value with the end of the text.</param>
	private static void AssertEndsWith(ref SpanStringBuilder builder, string text, string value, StringComparison comparison)
	{
		bool expected = text.Length > 0 && text.EndsWith(value, comparison);
		if (builder.EndsWith(value, comparison) != expected)
		{
			Assert.Fail($"EndsWith(\"{value}\", {comparison}) returned {!expected}.");
		}
	}

	/// <summary>
	/// Asserts that a change didn't shrink a builder's buffer, and that a change that asks for no more room than it
	/// writes grew the buffer only when the text no longer fit in it.
	/// </summary>
	/// <param name="builder">The builder that the change was made to.</param>
	/// <param name="change">The change.</param>
	/// <param name="capacityBefore">The builder's capacity before the change.</param>
	/// <param name="length">The length of the text after the change.</param>
	private static void AssertGrowth(ref SpanStringBuilder builder, Change change, int capacityBefore, int length)
	{
		if (builder.Capacity < capacityBefore)
		{
			Assert.Fail($"{change} shrank the buffer from {capacityBefore} characters to {builder.Capacity}.");
		}

		if (builder.Capacity > capacityBefore && length <= capacityBefore && !AsksForMoreRoomThanItWrites(change))
		{
			Assert.Fail($"{change} grew the buffer from {capacityBefore} characters, which held all {length} of the text.");
		}
	}

	/// <summary>
	/// Asserts that a builder is back to its default state, with no buffer and nothing rented, so that disposing it
	/// returns nothing to the pool.
	/// </summary>
	/// <param name="builder">The builder to check.</param>
	private static void AssertDefault(ref SpanStringBuilder builder)
	{
		Assert.True(builder.IsDefault, "The builder isn't back to its default state.");
		Assert.False(builder.IsRented, "The builder still holds the array that it rented.");
		Assert.Equal(0, builder.Length);
		Assert.Equal(0, builder.Capacity);
		Assert.Equal(string.Empty, builder.ToString());
	}

	/// <summary>
	/// The members that start a builder on the caller's span.
	/// </summary>
	private enum SpanStart
	{
		/// <summary>The constructor that takes a span.</summary>
		Constructor,
		/// <summary><see cref="SpanStringBuilder.Create"/>, as the library's callers start one.</summary>
		Create,
		/// <summary>The conversion from a span.</summary>
		Conversion,
	}

	/// <summary>
	/// The overloads that append what a callback writes into room that they take for it.
	/// </summary>
	private enum Writer
	{
		/// <summary>The overload that takes an <see cref="Action{T1, T2}"/>.</summary>
		Action,
		/// <summary>The overload that takes a <see cref="Func{T1, T2, TResult}"/>.</summary>
		Func,
		/// <summary>The overload that takes a pointer to a function that returns nothing.</summary>
		ActionPointer,
		/// <summary>The overload that takes a pointer to a function that returns how many characters it wrote.</summary>
		FuncPointer,
	}

	/// <summary>
	/// How a property starts a builder.
	/// </summary>
	private abstract record Start;

	private sealed record OnSpan(int Length, SpanStart How) : Start;

	private sealed record Rented(int Capacity, bool ThroughCreate) : Start;

	private sealed record FromDefault : Start;

	/// <summary>
	/// A change that the properties make to a builder.
	/// </summary>
	private abstract record Change;

	private sealed record AppendChar(char Value) : Change;

	private sealed record AppendRepeated(char Value, int Count) : Change;

	private sealed record AppendText(string Value) : Change;

	private sealed record AppendCharacters(char First, char Second) : Change;

	private sealed record AppendFormatted(ISpanFormattable Value, int MaxLength, string? Format, string? Culture) : Change;

	private sealed record AppendGuidText(Guid Value, string Format) : Change;

	private sealed record AppendEnum(FileAttributes Value) : Change;

	private sealed record AppendUtf8(string Hex) : Change;

	private sealed record AppendNewLine : Change;

	private sealed record AppendLineText(string Value) : Change;

	private sealed record AppendEnclosed(string Value, char Enclosure) : Change;

	private sealed record AppendFilled(int Length, char Value) : Change;

	private sealed record AppendWritten(int Length, char Value) : Change;

	private sealed record AppendWrittenThroughPointer(int Length, char Value) : Change;

	private sealed record AppendWrittenUpTo(int MaxLength, int Written, char Value) : Change;

	private sealed record AppendWrittenUpToThroughPointer(int MaxLength, int Written, char Value) : Change;

	private sealed record InsertChar(int Index, char Value) : Change;

	private sealed record InsertRepeated(int Index, char Value, int Count) : Change;

	private sealed record InsertText(int Index, string Value) : Change;

	private sealed record Overwrite(int Index, string Value) : Change;

	private sealed record OverwriteFilled(int Index, int Length, char Value) : Change;

	private sealed record RemoveRange(int Start, int Length) : Change;

	private sealed record SetChar(int Index, char Value) : Change;

	private sealed record RemoveLast : Change;

	private sealed record RemoveLastIf(char Value) : Change;

	private sealed record TrimTrailing(char Value) : Change;

	private sealed record ClearAll : Change;

	/// <summary>
	/// A call that the rejection property makes with an argument outside the range that its member takes. An
	/// <c>Offset</c> says how far outside the range the argument falls, as <see cref="Outside(int, int)"/> reads it, and
	/// an <c>At</c> gives the argument before it, inside its own range, as <see cref="Inside(int, int)"/> reads it.
	/// </summary>
	private abstract record Rejection;

	private sealed record CharAt(int Offset, bool Write) : Rejection;

	private sealed record NegativeCopies(int Count) : Rejection;

	private sealed record NoRoomToFormat(int MaxLength) : Rejection;

	private sealed record NegativeRoomToWrite(Writer Writer, int Length) : Rejection;

	private sealed record NullWriter(bool CountsWritten, int Length) : Rejection;

	private sealed record InvalidGuidFormat(string Format) : Rejection;

	private sealed record SearchFrom(int Offset, bool Counted) : Rejection;

	private sealed record SearchCounting(int At, int Offset) : Rejection;

	private sealed record InsertCharAt(int Offset) : Rejection;

	private sealed record InsertCopiesAt(int Offset, int Count) : Rejection;

	private sealed record InsertTextAt(int Offset, string Value) : Rejection;

	private sealed record NegativeInsertedCopies(int At, int Count) : Rejection;

	private sealed record OverwriteAt(int Offset, string Value) : Rejection;

	private sealed record OverwriteSpanAt(int Offset, int Length) : Rejection;

	private sealed record NegativeOverwriteSpan(int At, int Length) : Rejection;

	private sealed record RemoveFrom(int Offset) : Rejection;

	private sealed record RemoveCounting(int At, int Offset) : Rejection;

	private sealed record ShortDestination(int By) : Rejection;

	private sealed record SliceFrom(int Offset) : Rejection;

	private sealed record SliceCounting(int At, int Offset) : Rejection;

	private sealed record NegativeCapacity(int Capacity) : Rejection;
}
