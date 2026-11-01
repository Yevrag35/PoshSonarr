using MG.Sonarr.Next.Extensions.Strings;
using System.ComponentModel;

namespace MG.Sonarr.Next.Services.Http.Queries;

/// <summary>
/// Represents the paging and sorting fields of a query to one of Sonarr's paged endpoints, which
/// format as <c>page=1&amp;pageSize=10&amp;sortDirection=Descending&amp;sortKey=key</c>.
/// </summary>
/// <remarks>
/// <para>
///     The instance is mutable and isn't thread-safe. Its numbers are formatted with the module's
///     default format provider, whatever the caller passes, and the sort key is written as given,
///     with no URL escaping.
/// </para>
/// <para>
///     A new instance asks for the first page of 10 items, sorted in descending order, with an
///     empty sort key.
/// </para>
/// </remarks>
public sealed class PagingParameter : IQueryField
{
	const string PAGING_KEY = "Paging";
	private const string PAGE = "page";
	private const string PAGE_SIZE = "pageSize";
	private const string SORT_DIRECTION = "sortDirection";
	private const string SORT_KEY = "sortKey";

	const int DEFAULT_PAGE_NO = 1;
	const int DEFAULT_PAGE_SIZE = 10;
	static readonly int STARTING_LENGTH = GetStartingLength();

	private string _sortKey = string.Empty;

	/// <summary>
	/// Gets the key that identifies the paging fields in a query collection.
	/// </summary>
	/// <remarks>
	/// The key names the group of fields and isn't written when the instance is formatted.
	/// </remarks>
	/// <value>The string <c>Paging</c>.</value>
	public string Key => PAGING_KEY;
	/// <summary>
	/// Gets the largest number of characters that the instance writes when formatted.
	/// </summary>
	/// <value>
	///     The length of the four fields with the longest page number, page size, and sort
	///     direction, plus the length of <see cref="SortKey"/>.
	/// </value>
	public int MaxLength => STARTING_LENGTH + _sortKey.Length;
	/// <summary>
	/// Gets or sets the number of the page to get.
	/// </summary>
	/// <value>The one-based page number. The default is 1.</value>
	public int Page { get; set; } = DEFAULT_PAGE_NO;
	/// <summary>
	/// Gets or sets the number of items on each page.
	/// </summary>
	/// <value>The page size. The default is 10.</value>
	public int PageSize { get; set; } = DEFAULT_PAGE_SIZE;
	/// <summary>
	/// Gets or sets the key that Sonarr sorts the items by.
	/// </summary>
	/// <value>
	///     The sort key, or an empty string when none is set. Setting the property to
	///     <see langword="null"/> stores an empty string.
	/// </value>
	[NotNull]
	public string? SortKey
	{
		get => _sortKey ??= string.Empty;
		set => _sortKey = value ?? string.Empty;
	}
	/// <summary>
	/// Gets or sets the direction that Sonarr sorts the items in.
	/// </summary>
	/// <value>
	///     The sort direction, written by name, or as its number when it isn't a defined value. The
	///     default is <see cref="ListSortDirection.Descending"/>.
	/// </value>
	public ListSortDirection SortDirection { get; set; } = ListSortDirection.Descending;

	/// <summary>
	/// Returns the paging fields formatted as a query string fragment.
	/// </summary>
	/// <param name="format">Ignored; the fields have one format.</param>
	/// <param name="formatProvider">Ignored; the numbers are formatted with the module's default format provider.</param>
	/// <returns>The formatted fields, or as many of them as fit in <see cref="MaxLength"/> characters.</returns>
	[DebuggerStepThrough]
	string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
	{
		Span<char> chars = stackalloc char[this.MaxLength];
		_ = this.TryFormat(chars, out int charsWritten);
		return new(chars.Slice(0, charsWritten));
	}
	/// <summary>
	/// Tries to write the paging fields into the specified span, joined by <c>&amp;</c>.
	/// </summary>
	/// <param name="destination">The span to write the fields into.</param>
	/// <param name="charsWritten">
	///     When this method returns, the number of characters written to
	///     <paramref name="destination"/>, which counts the fields that fit before one didn't.
	/// </param>
	/// <returns>
	///     <see langword="true"/> if all four fields fit in <paramref name="destination"/>; otherwise,
	///     <see langword="false"/>.
	/// </returns>
	public bool TryFormat(Span<char> destination, out int charsWritten)
	{
		charsWritten = 0;
		return TryWriteSection(destination, ref charsWritten, PAGE, this.Page)
			&& TryWriteSection(destination, ref charsWritten, PAGE_SIZE, this.PageSize)
			&& TryWriteSection(destination, ref charsWritten, SORT_DIRECTION, this.SortDirection)
			&& TryWriteSection(destination, ref charsWritten, SORT_KEY, this.SortKey);
	}
	/// <summary>
	/// Tries to write a field with a number as its value, as <c>name=value</c>, at the specified
	/// position of the specified span.
	/// </summary>
	/// <param name="destination">The span to write the field into.</param>
	/// <param name="charsWritten">The position to write at, which advances past each part of the field that fits.</param>
	/// <param name="name">The field's name.</param>
	/// <param name="value">The field's value, formatted with the module's default format provider.</param>
	/// <returns>
	///     <see langword="true"/> if the whole field fits in <paramref name="destination"/>; otherwise,
	///     <see langword="false"/>.
	/// </returns>
	private static bool TryWriteSection(Span<char> destination, ref int charsWritten, ReadOnlySpan<char> name, int value)
	{
		if (!name.TryCopyToSlice(destination, ref charsWritten))
		{
			return false;
		}

		try
		{
			destination[charsWritten++] = '=';
			return value.TryCopyToSlice(destination, ref charsWritten, provider: Statics.DefaultProvider);
		}
		catch (IndexOutOfRangeException)
		{
			return false;
		}
	}
	/// <summary>
	/// Tries to write a field with a sort direction as its value, as <c>name=value</c>, at the
	/// specified position of the specified span.
	/// </summary>
	/// <param name="destination">The span to write the field into.</param>
	/// <param name="charsWritten">The position to write at, which advances past each part of the field that fits.</param>
	/// <param name="name">The field's name.</param>
	/// <param name="direction">
	///     The field's value, written by name, or as its number when it isn't a defined
	///     <see cref="ListSortDirection"/> value.
	/// </param>
	/// <returns>
	///     <see langword="true"/> if the whole field fits in <paramref name="destination"/>; otherwise,
	///     <see langword="false"/>.
	/// </returns>
	private static bool TryWriteSection(Span<char> destination, ref int charsWritten, ReadOnlySpan<char> name, ListSortDirection direction)
	{
		if (!name.TryCopyToSlice(destination, ref charsWritten))
		{
			return false;
		}

		try
		{
			destination[charsWritten++] = '=';
			return direction switch
			{
				ListSortDirection.Ascending => nameof(ListSortDirection.Ascending).AsSpan().TryCopyToSlice(destination, ref charsWritten),
				ListSortDirection.Descending => nameof(ListSortDirection.Descending).AsSpan().TryCopyToSlice(destination, ref charsWritten),
				_ => ((int)direction).TryCopyToSlice(destination, ref charsWritten, provider: Statics.DefaultProvider),
			};
		}
		catch (IndexOutOfRangeException)
		{
			return false;
		}
	}
	/// <summary>
	/// Tries to write a field with text as its value, as <c>name=value</c>, at the specified
	/// position of the specified span.
	/// </summary>
	/// <param name="destination">The span to write the field into.</param>
	/// <param name="charsWritten">The position to write at, which advances past each part of the field that fits.</param>
	/// <param name="name">The field's name.</param>
	/// <param name="value">The field's value, written as given.</param>
	/// <returns>
	///     <see langword="true"/> if the whole field fits in <paramref name="destination"/>; otherwise,
	///     <see langword="false"/>.
	/// </returns>
	private static bool TryWriteSection(Span<char> destination, ref int charsWritten, ReadOnlySpan<char> name, ReadOnlySpan<char> value)
	{
		if (!name.TryCopyToSlice(destination, ref charsWritten))
		{
			return false;
		}

		try
		{
			destination[charsWritten++] = '=';
			return value.TryCopyToSlice(destination, ref charsWritten);
		}
		catch (IndexOutOfRangeException)
		{
			return false;
		}
	}
	/// <summary>
	/// Tries to write the paging fields into the specified span, joined by <c>&amp;</c>.
	/// </summary>
	/// <param name="destination">The span to write the fields into.</param>
	/// <param name="charsWritten">When this method returns, the number of characters written to <paramref name="destination"/>.</param>
	/// <param name="format">Ignored; the fields have one format.</param>
	/// <param name="provider">Ignored; the numbers are formatted with the module's default format provider.</param>
	/// <returns>
	///     <see langword="true"/> if all four fields fit in <paramref name="destination"/>; otherwise,
	///     <see langword="false"/>.
	/// </returns>
	[DebuggerStepThrough]
	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		return this.TryFormat(destination, out charsWritten);
	}

	/// <summary>
	/// Returns the combined length of the specified field names with an equals sign after each and
	/// an <c>&amp;</c> between each pair.
	/// </summary>
	/// <param name="values">The field names.</param>
	/// <returns>The number of characters that the names and their separators take.</returns>
	[DebuggerStepThrough]
	static int CombineLengths(params ReadOnlySpan<string> values)
	{
		int length = 0;
		foreach (string value in values)
		{
			length += value.Length;
		}

		return length + values.Length + Math.Max(0, values.Length - 1);
	}
	/// <summary>
	/// Returns the length of the formatted fields without a sort key.
	/// </summary>
	/// <returns>
	///     The length of the four names and their separators, plus room for the longest
	///     <see cref="int"/> as the page number and the page size and for <c>Descending</c> as the
	///     sort direction.
	/// </returns>
	[DebuggerStepThrough]
	static int GetStartingLength()
	{
		return (LengthConstants.INT_MAX * 2) + 10 + CombineLengths(PAGE, PAGE_SIZE, SORT_DIRECTION, SORT_KEY);
	}
}
