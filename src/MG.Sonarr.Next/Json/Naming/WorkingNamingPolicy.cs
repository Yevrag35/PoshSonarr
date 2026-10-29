using MG.Sonarr.Next.Buffers;

namespace MG.Sonarr.Next.Json.Naming;

/// <summary>
/// Encapsulates a JSON naming policy along with its associated serializer options to efficiently convert
/// property names during JSON serialization.
/// </summary>
/// <remarks>
/// This class acts as a working container for a JSON naming policy. It supports both the standard
/// conversion mechanism (using <see cref="JsonNamingPolicy"/>) and a more efficient span-based conversion when
/// the naming policy implements <see cref="JsonSpanCamelCaseNamingPolicy"/>. When a naming policy is defined in
/// the provided <see cref="JsonSerializerOptions"/>, this class enables the conversion of property names
/// (typically to a format like camelCase) with minimal allocations by leveraging span-based operations.
/// </remarks>
[DebuggerDisplay(@"\{HasPolicy = {HasPolicy}, IsSpanPolicy = {IsSpanPolicy}, Policy = {Policy}\}")]
public sealed class WorkingNamingPolicy : JsonNamingPolicy
{
	private const int MAX_STACKALLOC = 128;

	/// <summary>
	/// Gets a value indicating whether a naming policy is present.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if a naming policy is present; otherwise, <see langword="false"/>.
	/// </value>
	[MemberNotNullWhen(true, nameof(Policy), nameof(Options))]
	public bool HasPolicy { get; }
	/// <summary>
	/// Gets a value indicating whether the naming policy is a span policy.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the naming policy is a span policy; otherwise, <see langword="false"/>.
	/// </value>
	[MemberNotNullWhen(true, nameof(Policy), nameof(_spanPolicy), nameof(Options))]
	public bool IsSpanPolicy { get; }

	/// <summary>
	/// Gets the JSON serializer options.
	/// </summary>
	public JsonSerializerOptions? Options { get; }

	private readonly JsonSpanCamelCaseNamingPolicy? _spanPolicy;
	/// <summary>
	/// Gets the JSON naming policy.
	/// </summary>
	public JsonNamingPolicy? Policy { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="WorkingNamingPolicy"/> struct with the specified JSON serializer options.
	/// </summary>
	/// <param name="options">The JSON serializer options.</param>
	public WorkingNamingPolicy(JsonSerializerOptions? options)
	{
		this.Options = options;
		JsonNamingPolicy? pol = options?.PropertyNamingPolicy;
		bool hasPol = pol is not null;
		if (hasPol && pol is JsonSpanCamelCaseNamingPolicy spanPolicy)
		{
			_spanPolicy = spanPolicy;
			this.IsSpanPolicy = true;
		}

		this.HasPolicy = hasPol;
		this.Policy = pol;
	}

	/// <inheritdoc/>
	public override string ConvertName(string name)
	{
		return this.HasPolicy ? this.Policy.ConvertName(name) : name;
	}
	/// <summary>
	/// Creates a new instance of <see cref="JsonNamingPolicyOverride"/> using the current options and the specified
	/// override setting.
	/// </summary>
	/// <param name="override">A value indicating whether to apply the override naming policy. Specify <see langword="true"/> to enable the
	/// override; otherwise, <see langword="false"/>.</param>
	/// <returns>A <see cref="JsonNamingPolicyOverride"/> instance configured with the provided override setting.</returns>
	public JsonNamingPolicyOverride GetOverrideNamingPolicy(bool @override)
	{
		return new(this.Options, @override);
	}
	internal void SetOverride(ref JsonSpanCamelCaseNamingPolicy? spanPolicy)
	{
		spanPolicy = _spanPolicy;
	}

	/// <summary>
	/// Converts the specified UTF-8 property name to a new name using the naming policy.
	/// </summary>
	/// <param name="utf8PropertyName">The UTF-8 property name.</param>
	/// <param name="buffer">The buffer to store the converted name.</param>
	/// <returns>The converted name as a read-only span of bytes.</returns>
	public ReadOnlySpan<byte> ConvertName(ReadOnlySpan<byte> utf8PropertyName, Span<byte> buffer)
	{
		if (this.IsSpanPolicy && utf8PropertyName.TryCopyTo(buffer))
		{
			return _spanPolicy.ConvertName(buffer);
		}

		if (!this.HasPolicy)
		{
			return utf8PropertyName;
		}

		int max = Encoding.UTF8.GetMaxCharCount(utf8PropertyName.Length);

		using (var chars = RentedBuffer.Rent<char>(
			max <= MAX_STACKALLOC
				? stackalloc char[max]
				: max))
		{
			int written = Encoding.UTF8.GetChars(utf8PropertyName, chars.Span);
			string name = this.Policy.ConvertName(new string(chars[..written]));

			if (Encoding.UTF8.GetMaxByteCount(name.Length) > buffer.Length)
			{
				Debug.Fail("An allocation happened AND the destination was too small anyway.");
				return Encoding.UTF8.GetBytes(name);
			}

			Debug.Fail("An allocation happened here ^");
			written = Encoding.UTF8.GetBytes(name, buffer);
			return buffer[..written];
		}
	}

	/// <summary>
	/// Tries to convert the specified property name to a new name using the naming policy.
	/// </summary>
	/// <param name="propertyName">The property name.</param>
	/// <param name="destination">The destination buffer to store the converted name.</param>
	/// <returns>
	/// <see langword="true"/> if the conversion was successful; otherwise, <see langword="false"/>.
	/// </returns>
	public bool TryConvertName([NotNullWhen(true)] string? propertyName, Span<char> destination)
	{
		if (!this.IsSpanPolicy || string.IsNullOrWhiteSpace(propertyName) || !propertyName.TryCopyTo(destination))
			return false;

		_spanPolicy.ConvertName(destination);
		return true;
	}

	/// <summary>
	/// Tries to convert the specified property name to a new name using the naming policy.
	/// </summary>
	/// <param name="propertyName">The property name.</param>
	/// <param name="destination">The destination buffer to store the converted name.</param>
	/// <returns>
	/// <see langword="true"/> if the conversion was successful; otherwise, <see langword="false"/>.
	/// </returns>
	public bool TryConvertName(ReadOnlySpan<char> propertyName, Span<char> destination)
	{
		if (!this.IsSpanPolicy || !propertyName.TryCopyTo(destination))
			return false;

		_spanPolicy.ConvertName(destination);
		return true;
	}

	/// <summary>
	/// Tries to convert the specified UTF-8 property name to a new name using the naming policy.
	/// </summary>
	/// <param name="utf8PropertyName">The UTF-8 property name.</param>
	/// <param name="destination">The destination buffer to store the converted name.</param>
	/// <returns>
	/// <see langword="true"/> if the conversion was successful; otherwise, <see langword="false"/>.
	/// </returns>
	public bool TryConvertName(ReadOnlySpan<byte> utf8PropertyName, Span<byte> destination)
	{
		if (!this.IsSpanPolicy || !utf8PropertyName.TryCopyTo(destination))
			return false;

		_spanPolicy.ConvertName(destination);
		return true;
	}

	/// <summary>
	/// Writes the property name to the specified <see cref="Utf8JsonWriter"/>.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="propertyName">The property name.</param>
	public void WritePropertyName(Utf8JsonWriter writer, string propertyName)
	{
		ArgumentException.ThrowIfNullOrEmpty(propertyName);
		if (this.HasPolicy)
		{
			if (this.IsSpanPolicy)
			{
				WriteCharSpan(writer, _spanPolicy, propertyName);
				return;
			}

			propertyName = this.Policy.ConvertName(propertyName);
		}

		writer.WritePropertyName(propertyName);
	}

	/// <summary>
	/// Writes the property name to the specified <see cref="Utf8JsonWriter"/>.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="propertyNameSpan">The property name as a read-only span of characters.</param>
	public void WritePropertyName(Utf8JsonWriter writer, ReadOnlySpan<char> propertyNameSpan)
	{
		if (this.HasPolicy)
		{
			if (this.IsSpanPolicy)
			{
				WriteCharSpan(writer, _spanPolicy, propertyNameSpan);
				return;
			}

			propertyNameSpan = this.Policy.ConvertName(propertyNameSpan.ToString());
			Debug.Fail("An allocation happened here ^");
		}

		writer.WritePropertyName(propertyNameSpan);
	}

	/// <summary>
	/// Writes the UTF-8 encoded property name to the specified <see cref="Utf8JsonWriter"/>.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="propertyName">The property name as a read-only span of bytes.</param>
	public void WritePropertyName(Utf8JsonWriter writer, ReadOnlySpan<byte> propertyName)
	{
		if (!this.HasPolicy)
		{
			writer.WritePropertyName(propertyName);
			return;
		}
		else if (!this.IsSpanPolicy)
		{
			writeUsingNonSpanPolicy(this.Policy, writer, propertyName);
			return;
		}

		using (var tempSpan = RentedBuffer.Rent<byte>(
				propertyName.Length <= MAX_STACKALLOC
					? stackalloc byte[propertyName.Length]
					: propertyName.Length))
		{
			propertyName.CopyTo(tempSpan.Span);
			writer.WritePropertyName(_spanPolicy.ConvertName(tempSpan.Span));
			return;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static void writeUsingNonSpanPolicy(JsonNamingPolicy policy, Utf8JsonWriter writer, ReadOnlySpan<byte> propertyName)
		{
			int length = Encoding.UTF8.GetMaxCharCount(propertyName.Length);

			using (var chars = RentedBuffer.Rent<char>(
				length <= MAX_STACKALLOC
					? stackalloc char[length]
					: length))
			{
				int written = Encoding.UTF8.GetChars(propertyName, chars.Span);
				writer.WritePropertyName(policy.ConvertName(new string(chars[..written])));

				Debug.Fail("An allocation happened here ^");
			}
		}
	}

	/// <summary>
	/// Writes the property name to the specified <see cref="Utf8JsonWriter"/> using the specified span policy.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="spanPolicy">The span policy.</param>
	/// <param name="propertyName">The property name as a read-only span of characters.</param>
	private static void WriteCharSpan(Utf8JsonWriter writer, JsonSpanCamelCaseNamingPolicy spanPolicy, ReadOnlySpan<char> propertyName)
	{
		int maxLength = Encoding.UTF8.GetMaxByteCount(propertyName.Length);
		using (var buffer = RentedBuffer.Rent<byte>(
			maxLength <= MAX_STACKALLOC
				? stackalloc byte[maxLength]
				: maxLength))
		{
			int written = Encoding.UTF8.GetBytes(propertyName, buffer.Span);
			Span<byte> writeSpan = spanPolicy.ConvertName(buffer[..written]);

			writer.WritePropertyName(writeSpan);
		}
	}
}
