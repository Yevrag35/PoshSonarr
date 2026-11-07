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
	/// The text that the changes write: none, short text, text outside ASCII, and text longer than any buffer that the
	/// builder starts with.
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
	/// The changes that the property makes, which cover every member that writes to the builder.
	/// </summary>
	private static readonly Gen<Change> s_changes = Gen.OneOf<Change>(
		s_chars.Select(value => (Change)new AppendChar(value)),
		Gen.Select(s_chars, Gen.Int[0, 300], (value, count) => (Change)new AppendRepeated(value, count)),
		s_text.Select(value => (Change)new AppendText(value)),
		Gen.Select(s_formattables, Gen.Int[1, 80], Gen.OneOf<string?>(null, "", "fr-FR", "th-TH"),
			(formattable, maxLength, culture) => (Change)new AppendFormatted(formattable.Value, maxLength, formattable.Format, culture)),
		Gen.Select(Gen.Guid, Gen.OneOf("", "N", "D", "B", "P", "X"), (value, format) => (Change)new AppendGuidText(value, format)),
		Gen.Int[0, 0x7FFFF].Select(value => (Change)new AppendEnum((FileAttributes)value)),
		Gen.Byte.Array[0, 40].Select(bytes => (Change)new AppendUtf8(Convert.ToHexString(bytes))),
		s_text.Select(value => (Change)new AppendUtf8(Convert.ToHexString(Encoding.UTF8.GetBytes(value)))),
		Gen.Const<Change>(new AppendNewLine()),
		s_text.Select(value => (Change)new AppendLineText(value)),
		Gen.Select(s_text, s_chars, (value, enclosure) => (Change)new AppendEnclosed(value, enclosure)),
		Gen.Select(Gen.Int[0, 300], s_chars, (length, value) => (Change)new AppendFilled(length, value)),
		Gen.Select(Gen.Int[0, 300], s_chars, (length, value) => (Change)new AppendWritten(length, value)),
		Gen.Select(Gen.Int[0, 100], Gen.Int[-2, 102], s_chars, (maxLength, written, value) => (Change)new AppendWrittenUpTo(maxLength, written, value)),
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
	/// The characters that <see cref="SpanStringBuilder.ContainsAny(SearchValues{char})"/> looks for.
	/// </summary>
	private static readonly SearchValues<char> s_searchValues = SearchValues.Create("é\n");

	/// <summary>
	/// A <see cref="SpanStringBuilder"/> holds the same text as a <see cref="StringBuilder"/> that the same changes are
	/// made to, whether it starts in a span of any length on the stack or in a rented array, through every growth into a
	/// larger rented array. A value that doesn't fit in the length that its caller allows throws a
	/// <see cref="FormatException"/> and leaves the text as it was. Each search finds what it finds in the text, and
	/// <see cref="SpanStringBuilder.EndsWith(ReadOnlySpan{char}, StringComparison)"/> is <see langword="false"/> for an
	/// empty builder, as its docs say.
	/// </summary>
	[Fact]
	public void HoldsTheTextThatAStringBuilderHoldsAfterTheSameChanges()
	{
		Gen.Select(Gen.Int[0, 64], Gen.Int[-1, 300], s_changes.Array[0, 30], s_chars).Sample((stackLength, rentedCapacity, changes, probe) =>
		{
			Span<char> stack = stackalloc char[stackLength];
			SpanStringBuilder builder = rentedCapacity < 0
				? new SpanStringBuilder(stack)
				: new SpanStringBuilder(rentedCapacity);

			StringBuilder model = new();
			foreach (Change change in changes)
			{
				Apply(ref builder, model, change);
				AssertHolds(ref builder, model.ToString(), probe);
			}

			Assert.Equal(model.ToString(), builder.Build());
			Assert.Equal(0, builder.Length);
		}, iter: 2_000, print: t => string.Create(
			CultureInfo.InvariantCulture,
			$"starting in {(t.Item2 < 0 ? $"{t.Item1} characters on the stack" : $"a rented array of at least {t.Item2}")}, with {string.Join(", ", t.Item3)}, probing for U+{(int)t.Item4:X4}"));
	}

	/// <summary>
	/// Makes a change to a builder and to the <see cref="StringBuilder"/> that models it.
	/// </summary>
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
				builder.Append(writeLength, value, static (span, state) => span.Fill(state));
				_ = model.Append(value, writeLength);
				break;

			case AppendWrittenUpTo(int maxLength, int written, char value):
				builder.Append(maxLength, (Value: value, Written: written), static (span, state) =>
				{
					span[..Math.Clamp(state.Written, 0, span.Length)].Fill(state.Value);
					return state.Written;
				});
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
	/// Asserts that a builder holds the specified text, and that each of its searches finds in it what the same search
	/// finds in the text.
	/// </summary>
	private static void AssertHolds(ref SpanStringBuilder builder, string text, char probe)
	{
		Assert.Equal(text, builder.ToString());
		Assert.Equal(text.Length, builder.Length);
		Assert.True(builder.Capacity >= builder.Length, "The builder holds more characters than its capacity.");
		Assert.Equal(text, builder.AsSpan().ToString());

		char[] copy = new char[text.Length];
		Assert.Equal(text.Length, builder.CopyTo(copy));
		Assert.Equal(text, new string(copy));

		int middle = text.Length / 2;
		Assert.Equal(text.IndexOf(probe), builder.IndexOf(probe));
		Assert.Equal(text.IndexOf(probe, middle), builder.IndexOf(probe, middle));
		Assert.Equal(text.IndexOf(probe, middle, text.Length - middle), builder.IndexOf(probe, middle, text.Length - middle));
		Assert.Equal(text.AsSpan().ContainsAny(s_searchValues), builder.ContainsAny(s_searchValues));

		string tail = text[middle..];
		Assert.Equal(text.Length > 0, builder.EndsWith(tail));
		Assert.Equal(text.Length > 0 && text.EndsWith(probe), builder.EndsWith(new string(probe, 1)));

		if (text.Length > 0)
		{
			Assert.Equal(text[middle], builder[middle]);
		}
	}

	/// <summary>
	/// A change that the property makes to a builder.
	/// </summary>
	private abstract record Change;

	private sealed record AppendChar(char Value) : Change;

	private sealed record AppendRepeated(char Value, int Count) : Change;

	private sealed record AppendText(string Value) : Change;

	private sealed record AppendFormatted(ISpanFormattable Value, int MaxLength, string? Format, string? Culture) : Change;

	private sealed record AppendGuidText(Guid Value, string Format) : Change;

	private sealed record AppendEnum(FileAttributes Value) : Change;

	private sealed record AppendUtf8(string Hex) : Change;

	private sealed record AppendNewLine : Change;

	private sealed record AppendLineText(string Value) : Change;

	private sealed record AppendEnclosed(string Value, char Enclosure) : Change;

	private sealed record AppendFilled(int Length, char Value) : Change;

	private sealed record AppendWritten(int Length, char Value) : Change;

	private sealed record AppendWrittenUpTo(int MaxLength, int Written, char Value) : Change;

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
}
