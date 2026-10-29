using MG.Sonarr.Next.Buffers;

namespace MG.Sonarr.Next.Json.Naming;

/// <summary>
/// Encapsulates a JSON naming policy along with its associated serializer options to efficiently convert
/// property names during JSON serialization.
/// </summary>
/// <remarks>
/// This readonly ref struct acts as a working container for a JSON naming policy. It supports both the standard
/// conversion mechanism (using <see cref="JsonNamingPolicy"/>) and a more efficient span-based conversion when
/// the naming policy implements <see cref="JsonSpanCamelCaseNamingPolicy"/>. When a naming policy is defined in
/// the provided <see cref="JsonSerializerOptions"/>, this struct enables the conversion of property names
/// (typically to a format like camelCase) with minimal allocations by leveraging span-based operations.
/// <para>
/// If no naming policy is provided, property names remain unchanged. Because this struct is declared as a ref struct,
/// its lifetime is strictly limited to the stack, preventing accidental storage on the heap.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay(@"\{HasPolicy = {HasPolicy}, IsSpanPolicy = {IsSpanPolicy}, Policy = {Policy}\}")]
public readonly ref struct JsonNamingPolicyOverride
{
	private const int MAX_STACKALLOC = 128;
	private readonly JsonSpanCamelCaseNamingPolicy? _spanPolicy;

	/// <summary>
	/// Gets a value indicating whether a naming policy is present.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if a naming policy is present; otherwise, <see langword="false"/>.
	/// </value>
	[MemberNotNullWhen(true, nameof(Policy), nameof(Options))]
	public readonly bool HasPolicy { get; }

	/// <summary>
	/// Gets the JSON serializer options.
	/// </summary>
	public readonly JsonSerializerOptions? Options;

	/// <summary>
	/// Gets a value indicating whether the naming policy is a span policy.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the naming policy is a span policy; otherwise, <see langword="false"/>.
	/// </value>
	[MemberNotNullWhen(true, nameof(Policy), nameof(_spanPolicy), nameof(Options))]
	public readonly bool IsSpanPolicy { get; }

	/// <summary>
	/// Gets the JSON naming policy.
	/// </summary>
	public readonly JsonNamingPolicy? Policy;

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonNamingPolicyOverride"/> struct with the specified JSON serializer options.
	/// </summary>
	/// <param name="options">The JSON serializer options.</param>
	/// <param name="override">When <see langword="true"/>, ignores the property naming policy from <paramref name="options"/> and applies no naming transformation.</param>
	public JsonNamingPolicyOverride(JsonSerializerOptions? options, bool @override)
	{
		Options = options;
		JsonNamingPolicy? pol = !@override ? options?.PropertyNamingPolicy : null;
		bool hasPol = pol is not null;
		if (hasPol && pol is JsonSpanCamelCaseNamingPolicy spanPolicy)
		{
			_spanPolicy = spanPolicy;
			this.IsSpanPolicy = true;
		}

		this.HasPolicy = hasPol;
		Policy = pol;
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="JsonNamingPolicyOverride"/> struct using the specified naming policy and override
	/// flag.
	/// </summary>
	/// <param name="policy">The WorkingNamingPolicy instance that provides the naming options and policy to apply.</param>
	/// <param name="override">A value indicating whether to override the provided naming policy. If <see langword="true"/>, the override is
	/// applied and the original policy is ignored.</param>
	internal JsonNamingPolicyOverride(WorkingNamingPolicy policy, bool @override)
	{
		Options = policy.Options;
		policy.SetOverride(ref _spanPolicy);
		this.HasPolicy = !@override && policy.HasPolicy;
		this.IsSpanPolicy = !@override && policy.IsSpanPolicy;
		Policy = policy.Policy;
	}

	/// <summary>
	/// Converts the specified UTF-8 property name to a new name using the naming policy.
	/// </summary>
	/// <param name="utf8PropertyName">The UTF-8 property name.</param>
	/// <param name="buffer">The buffer to store the converted name.</param>
	/// <returns>The converted name as a read-only span of bytes.</returns>
	public readonly ReadOnlySpan<byte> ConvertName(ReadOnlySpan<byte> utf8PropertyName, Span<byte> buffer)
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
			string name = Policy.ConvertName(new string(chars[..written]));

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
	public readonly bool TryConvertName([NotNullWhen(true)] string? propertyName, Span<char> destination)
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
	public readonly bool TryConvertName(ReadOnlySpan<char> propertyName, Span<char> destination)
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
	public readonly bool TryConvertName(ReadOnlySpan<byte> utf8PropertyName, Span<byte> destination)
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
	public readonly void WritePropertyName(Utf8JsonWriter writer, string propertyName)
	{
		ArgumentException.ThrowIfNullOrEmpty(propertyName);
		if (this.HasPolicy)
		{
			if (this.IsSpanPolicy)
			{
				WriteCharSpan(writer, _spanPolicy, propertyName);
				return;
			}

			propertyName = Policy.ConvertName(propertyName);
		}

		writer.WritePropertyName(propertyName);
	}

	/// <summary>
	/// Writes the property name to the specified <see cref="Utf8JsonWriter"/>.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="propertyNameSpan">The property name as a read-only span of characters.</param>
	public readonly void WritePropertyName(Utf8JsonWriter writer, ReadOnlySpan<char> propertyNameSpan)
	{
		//EmptyStructException.ThrowIf(propertyNameSpan.IsEmpty, propertyNameSpan);
		if (this.HasPolicy)
		{
			if (this.IsSpanPolicy)
			{
				WriteCharSpan(writer, _spanPolicy, propertyNameSpan);
				return;
			}

			propertyNameSpan = Policy.ConvertName(new(propertyNameSpan));
			Debug.Fail("An allocation happened here ^");
		}

		writer.WritePropertyName(propertyNameSpan);
	}

	/// <summary>
	/// Writes the UTF-8 encoded property name to the specified <see cref="Utf8JsonWriter"/>.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="propertyName">The property name as a read-only span of bytes.</param>
	public readonly void WritePropertyName(Utf8JsonWriter writer, ReadOnlySpan<byte> propertyName)
	{
		//EmptyStructException.ThrowIf(propertyName.IsEmpty, propertyName);
		if (!this.HasPolicy)
		{
			writer.WritePropertyName(propertyName);
			return;
		}
		else if (this.IsSpanPolicy)
		{
			using (var tempSpan = RentedBuffer.Rent<byte>(
				propertyName.Length <= MAX_STACKALLOC
					? stackalloc byte[propertyName.Length]
					: propertyName.Length))
			{
				propertyName.CopyTo(tempSpan.Span);
				writer.WritePropertyName(_spanPolicy.ConvertName(tempSpan.Span));
				return;
			}
		}

		int length = Encoding.UTF8.GetMaxCharCount(propertyName.Length);

		using (var chars = RentedBuffer.Rent<char>(
			length <= MAX_STACKALLOC
				? stackalloc char[length]
				: length))
		{
			int written = Encoding.UTF8.GetChars(propertyName, chars.Span);
			writer.WritePropertyName(Policy.ConvertName(new string(chars[..written])));
			Debug.Fail("An allocation happened here ^");
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
			scoped var span = buffer.Span;
			int written = Encoding.UTF8.GetBytes(propertyName, span);
			span = buffer.Span.UnsafeSlice(0, written);

			spanPolicy.ConvertName(span);
			writer.WritePropertyName(span);
		}
	}
}
