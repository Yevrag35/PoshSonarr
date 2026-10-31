using MG.Sonarr.Next.Models;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Json.Converters;

/// <summary>
/// Provides the converter that reads and writes one model type, through the connection's shared
/// <see cref="ObjectConverter"/> and the model's own name maps.
/// </summary>
/// <remarks>
/// <para>
/// A connection creates one for each model class marked for a converter of its own. The converter claims only
/// <typeparamref name="T"/> itself, not a type derived from it.
/// </para>
/// <para>
/// The constructor reads the model's <see cref="ISerializableNames{T}"/> maps once, so a map that the model changes
/// later doesn't reach the converter.
/// </para>
/// </remarks>
/// <typeparam name="T">The model type that the converter reads and writes.</typeparam>
public sealed class SonarrObjectConverter<T> : ByPolicyJsonConverter<T> where T : SonarrObject, ISerializableNames<T>, new()
{
	/// <summary>
	/// The shared converter that reads and writes the model's properties.
	/// </summary>
	readonly ObjectConverter _converter;
	/// <summary>
	/// The model's renames of JSON fields to property names.
	/// </summary>
	readonly IReadOnlyDictionary<string, string> _deserializedNames;
	/// <summary>
	/// The names of the string properties whose values get an uppercase first letter.
	/// </summary>
	readonly IReadOnlySet<string> _capitalizeProperties;
	/// <summary>
	/// The model's renames of property names back to JSON fields.
	/// </summary>
	readonly IReadOnlyDictionary<string, string> _serializedNames;

	/// <summary>
	/// Initializes a new <see cref="SonarrObjectConverter{T}"/> instance around the connection's shared object
	/// converter.
	/// </summary>
	/// <param name="converter">
	/// The shared converter that reads and writes the model's properties, whose naming policy this converter uses.
	/// </param>
	/// <exception cref="NullReferenceException">Thrown when <paramref name="converter"/> is null.</exception>
	public SonarrObjectConverter(ObjectConverter converter)
		: base(converter.NamingPolicy)
	{
		_converter = converter;
		_deserializedNames = T.GetDeserializedNames();
		_capitalizeProperties = T.GetPropertiesToCapitalize();
		_serializedNames = T.GetSerializedNames();
	}

	/// <summary>
	/// Reads a JSON object as a model and calls its <see cref="SonarrObject.OnDeserialized()"/>.
	/// </summary>
	/// <remarks>
	/// The model isn't tagged here. The HTTP client tags what a request returns.
	/// </remarks>
	/// <param name="reader">The reader positioned at the start of the object.</param>
	/// <param name="typeToConvert">The type to convert the value to.</param>
	/// <param name="options">The serializer options that nested values are read with.</param>
	/// <returns>The model, with a property for each of the object's fields.</returns>
	/// <exception cref="JsonException">
	/// Thrown when the reader isn't positioned at the start of a JSON object, or when a value can't be read.
	/// </exception>
	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException($"Converter expected a JSON object but got {reader.TokenType}.");
		}

		T obj = _converter.ConvertToObject<T>(ref reader, options, _deserializedNames, _capitalizeProperties);
		obj.OnDeserialized();
		return obj;
	}

	/// <summary>
	/// Writes a model's note properties as a JSON object, or a JSON null for a <see langword="null"/> model.
	/// </summary>
	/// <remarks>
	/// A model that implements <see cref="IJsonOnSerializing"/> gets its <see cref="IJsonOnSerializing.OnSerializing"/>
	/// called first, so it can put back the properties that a request body needs.
	/// </remarks>
	/// <param name="writer">The writer to write the object to.</param>
	/// <param name="value">The model to write.</param>
	/// <param name="options">The serializer options that nested values are written with.</param>
	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		if (value is IJsonOnSerializing serializing)
		{
			serializing.OnSerializing();
		}

		if (!writer.TryWriteNullValue(value))
		{
			_converter.WritePSObject(writer, options, value, _serializedNames);
		}
	}
}
