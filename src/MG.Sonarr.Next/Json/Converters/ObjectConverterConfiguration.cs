using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Json.Converters.Spans;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Json.Converters;

/// <summary>
/// Represents the settings that an <see cref="ObjectConverter"/> reads and writes objects with, as its constructor's
/// callback receives them.
/// </summary>
/// <remarks>
/// Every name that the settings hold is a property's name after its first letter is uppercased, and names compare
/// without regard to case.
/// </remarks>
public interface IObjectConverterConfig
{
	/// <summary>
	/// Adds the properties whose JSON arrays are read as a specific type, instead of as an array of objects.
	/// </summary>
	/// <param name="pairs">The pairs of a property's name and the type to read its array as.</param>
	/// <returns>The same settings, so that calls can be chained.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="pairs"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when a property's name was already added.</exception>
	IObjectConverterConfig AddConvertProperties(IEnumerable<KeyValuePair<string, Type>> pairs);
	/// <summary>
	/// Sets the property renames that apply to every object, unless the object's own model renames the property.
	/// </summary>
	/// <param name="pairs">
	/// The pairs of a JSON field's name, once its first letter is uppercased, and the property's name. The reverse
	/// of each pair renames the property when it's written.
	/// </param>
	/// <returns>The same settings, so that calls can be chained.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="pairs"/> is null.</exception>
	IObjectConverterConfig AddGlobalReplaceNames(IEnumerable<KeyValuePair<string, string>> pairs);
	/// <summary>
	/// Adds the properties that are left out when an object is written.
	/// </summary>
	/// <param name="names">The names of the properties to leave out.</param>
	/// <returns>The same settings, so that calls can be chained.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="names"/> is null.</exception>
	IObjectConverterConfig AddIgnoreProperties(IEnumerable<string> names);
	/// <summary>
	/// Adds the span converters that read the string values of specific properties, in place of the converter's
	/// own parsing.
	/// </summary>
	/// <param name="converterPairs">The pairs of a property's name and the span converter that reads its values.</param>
	/// <returns>The same settings, so that calls can be chained.</returns>
	/// <exception cref="ArgumentException">Thrown when a property's name was already added.</exception>
	IObjectConverterConfig AddSpanConverters(params ReadOnlySpan<KeyValuePair<string, SpanConverter>> converterPairs);
}

/// <summary>
/// Provides the settings of an <see cref="ObjectConverter"/>, which its constructor's callback fills.
/// </summary>
internal sealed class ObjectConverterConfiguration : IObjectConverterConfig
{
	/// <summary>
	/// The types that properties' arrays are read as, by property name.
	/// </summary>
	readonly Dictionary<string, Type> _convertProps;
	/// <summary>
	/// The property renames that apply to every object.
	/// </summary>
	JsonNameHolder _globalReplaceNames;
	/// <summary>
	/// The names of the properties that are left out when an object is written.
	/// </summary>
	readonly HashSet<string> _ignoreProps;
	/// <summary>
	/// The span converters that read properties' string values, by property name.
	/// </summary>
	readonly Dictionary<string, SpanConverter> _spanConverters;

	/// <summary>
	/// Gets the types that properties' arrays are read as.
	/// </summary>
	/// <value>A dictionary of types, keyed by property name without regard to case.</value>
	internal IReadOnlyDictionary<string, Type> ConvertProperties => _convertProps;
	/// <summary>
	/// Gets the property renames that apply to every object.
	/// </summary>
	/// <value>The renames, or empty maps when none were set.</value>
	internal JsonNameHolder GlobalReplaceNames => _globalReplaceNames;
	/// <summary>
	/// Gets the names of the properties that are left out when an object is written.
	/// </summary>
	/// <value>A set of property names, compared without regard to case.</value>
	internal IReadOnlySet<string> IgnoreProperties => _ignoreProps;
	/// <summary>
	/// Gets the resolver that nested models are tagged with.
	/// </summary>
	/// <value>The resolver that holds the tags that the connection registered.</value>
	internal IMetadataResolver Resolver { get; }
	/// <summary>
	/// Gets the span converters that read properties' string values.
	/// </summary>
	/// <value>A dictionary of span converters, keyed by property name without regard to case.</value>
	internal IReadOnlyDictionary<string, SpanConverter> SpanConverters => _spanConverters;

	/// <summary>
	/// Initializes a new <see cref="ObjectConverterConfiguration"/> instance with empty settings and the resolver that
	/// nested models are tagged with.
	/// </summary>
	/// <param name="resolver">The resolver that holds the tags that the connection registered.</param>
	internal ObjectConverterConfiguration(IMetadataResolver resolver)
	{
		this.Resolver = resolver;
		_convertProps = new(StringComparer.OrdinalIgnoreCase);
		_globalReplaceNames = default;
		_ignoreProps = new(_convertProps.Comparer);
		_spanConverters = new(_convertProps.Comparer);
	}

	/// <summary>
	/// Adds the properties whose JSON arrays are read as a specific type.
	/// </summary>
	/// <param name="pairs">The pairs of a property's name and the type to read its array as.</param>
	/// <returns>This instance, so that calls can be chained.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="pairs"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when a property's name was already added.</exception>
	public IObjectConverterConfig AddConvertProperties(IEnumerable<KeyValuePair<string, Type>> pairs)
	{
		ArgumentNullException.ThrowIfNull(pairs);
		foreach (var pair in pairs)
		{
			_convertProps.Add(pair.Key, pair.Value);
		}

		return this;
	}
	/// <summary>
	/// Sets the property renames that apply to every object, replacing any that an earlier call set.
	/// </summary>
	/// <param name="pairs">
	/// The pairs of a JSON field's name, once its first letter is uppercased, and the property's name.
	/// </param>
	/// <returns>This instance, so that calls can be chained.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="pairs"/> is null.</exception>
	public IObjectConverterConfig AddGlobalReplaceNames(IEnumerable<KeyValuePair<string, string>> pairs)
	{
		ArgumentNullException.ThrowIfNull(pairs);
		_globalReplaceNames = JsonNameHolder.FromDeserializationNamePairs(pairs);
		return this;
	}
	/// <summary>
	/// Adds the properties that are left out when an object is written.
	/// </summary>
	/// <param name="names">The names of the properties to leave out.</param>
	/// <returns>This instance, so that calls can be chained.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="names"/> is null.</exception>
	public IObjectConverterConfig AddIgnoreProperties(IEnumerable<string> names)
	{
		ArgumentNullException.ThrowIfNull(names);
		_ignoreProps.UnionWith(names);
		return this;
	}
	/// <summary>
	/// Adds the span converters that read the string values of specific properties.
	/// </summary>
	/// <param name="converterPairs">
	/// The pairs of a property's name and the span converter that reads its values, which must not be empty.
	/// </param>
	/// <returns>This instance, so that calls can be chained.</returns>
	/// <exception cref="ArgumentException">Thrown when a property's name was already added.</exception>
	public IObjectConverterConfig AddSpanConverters(params ReadOnlySpan<KeyValuePair<string, SpanConverter>> converterPairs)
	{
		Debug.Assert(!converterPairs.IsEmpty);
		foreach (var kvp in converterPairs)
		{
			_spanConverters.Add(kvp.Key, kvp.Value);
		}

		return this;
	}
}
