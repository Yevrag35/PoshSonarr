using MG.Sonarr.Next.Models.Errors;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Collections;

/// <summary>
/// Represents a collection of error responses from a Sonarr server instance.
/// </summary>
public interface IErrorCollection : IReadOnlyList<SonarrServerError>
{
	/// <summary>
	/// Builds a single message by joining non-empty error messages with <see cref="Environment.NewLine"/>.
	/// </summary>
	/// <returns>The combined error message text.</returns>
	string BuildMessage();
}

/// <summary>
/// Represents an empty read-only <see cref="IErrorCollection"/> implementation.
/// </summary>
internal sealed class EmptyReadOnlyErrorCollection : IErrorCollection
{
	/// <summary>
	/// Gets the error at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index.</param>
	/// <value>The error at <paramref name="index"/>.</value>
	public SonarrServerError this[int index] => null!;

	/// <summary>
	/// Gets the number of errors in the collection.
	/// </summary>
	/// <value>The value <c>0</c>.</value>
	public int Count => 0;

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An empty enumerator.</returns>
	public IEnumerator<SonarrServerError> GetEnumerator()
	{
		return Enumerable.Empty<SonarrServerError>().GetEnumerator();
	}

	/// <inheritdoc cref="IErrorCollection.BuildMessage"/>
	public string BuildMessage()
	{
		return string.Empty;
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An empty enumerator.</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}
}

/// <summary>
/// Represents a mutable list-backed implementation of <see cref="IErrorCollection"/>.
/// </summary>
internal sealed class ErrorCollection : List<SonarrServerError>, IErrorCollection
{
	/// <summary>
	/// Initializes a new <see cref="ErrorCollection"/> instance.
	/// </summary>
	[JsonConstructor]
	public ErrorCollection()
		: base(1)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="ErrorCollection"/> instance with a single error.
	/// </summary>
	/// <param name="one">The single error to include.</param>
	private ErrorCollection(SonarrServerError one)
		: this()
	{
		this.Add(one);
	}

	/// <summary>
	/// Initializes a new <see cref="ErrorCollection"/> instance with the specified errors.
	/// </summary>
	/// <param name="errors">The source errors.</param>
	public ErrorCollection(IEnumerable<SonarrServerError> errors)
		: base(errors)
	{
	}

	/// <summary>
	/// Represents a static empty collection of errors.
	/// </summary>
	public static readonly IErrorCollection Empty = new EmptyReadOnlyErrorCollection();

	/// <summary>
	/// Creates an <see cref="IErrorCollection"/> from a single error value.
	/// </summary>
	/// <param name="one">The error value.</param>
	/// <returns>A collection containing <paramref name="one"/>, or <see cref="Empty"/> when <paramref name="one"/> is null.</returns>
	internal static IErrorCollection FromOne(SonarrServerError? one)
	{
		return one is not null
			? new ErrorCollection(one)
			: Empty;
	}

	/// <inheritdoc cref="IErrorCollection.BuildMessage"/>
	public string BuildMessage()
	{
		if (this.Count <= 0)
		{
			return string.Empty;
		}

		return string.Join(Environment.NewLine,
			this
				.Where(x => !string.IsNullOrEmpty(x.Message))
					.Select(x => x.Message));
	}
}
