using System.Text;

namespace MG.Sonarr.Next.Json.Naming;

[StructLayout(LayoutKind.Auto)]
public readonly ref struct WorkingNamingPolicy
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly JsonSpanCamelCaseNamingPolicy? _spanPolicy;

    [MemberNotNullWhen(true, nameof(Policy))]
    public readonly bool HasPolicy { get; }
    [MemberNotNullWhen(true, nameof(Policy), nameof(_spanPolicy))]
    public readonly bool IsSpanPolicy { get; }
    public readonly JsonNamingPolicy? Policy;

    [DebuggerStepThrough]
    public WorkingNamingPolicy(JsonSerializerOptions? options)
    {
        JsonNamingPolicy? pol = options?.PropertyNamingPolicy;
        bool hasPol = options is not null;
        if (hasPol && pol is JsonSpanCamelCaseNamingPolicy spanPolicy)
        {
            _spanPolicy = spanPolicy;
            this.IsSpanPolicy = true;
        }

        this.HasPolicy = hasPol;
        Policy = pol;
    }

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
    public readonly void WritePropertyName(Utf8JsonWriter writer, ReadOnlySpan<char> propertyNameSpan)
    {
        EmptyStructException.ThrowIf(propertyNameSpan.IsEmpty, typeof(ReadOnlySpan<char>));
        if (this.HasPolicy)
        {
            if (this.IsSpanPolicy)
            {
                WriteCharSpan(writer, _spanPolicy, propertyNameSpan);
                return;
            }

            propertyNameSpan = Policy.ConvertName(propertyNameSpan.ToString());
            Debug.Fail("An allocation happened here ^");
        }

        writer.WritePropertyName(propertyNameSpan);
    }
    public readonly void WritePropertyName(Utf8JsonWriter writer, ReadOnlySpan<byte> propertyName)
    {
        EmptyStructException.ThrowIf(propertyName.IsEmpty, typeof(ReadOnlySpan<byte>));
        if (!this.HasPolicy)
        {
            writer.WritePropertyName(propertyName);
            return;
        }
        else if (this.IsSpanPolicy)
        {
            Span<byte> tempSpan = stackalloc byte[propertyName.Length];
            propertyName.CopyTo(tempSpan);
            writer.WritePropertyName(_spanPolicy.ConvertSpan(tempSpan));
            return;
        }

        int length = Encoding.UTF8.GetMaxCharCount(propertyName.Length);
        Span<char> chars = stackalloc char[length];
        int written = Encoding.UTF8.GetChars(propertyName, chars);
        writer.WritePropertyName(Policy.ConvertName(chars.Slice(0, written).ToString()));
        Debug.Fail("An allocation happened here ^");
    }
    private static void WriteCharSpan(Utf8JsonWriter writer, JsonSpanCamelCaseNamingPolicy spanPolicy, ReadOnlySpan<char> propertyName)
    {
        Span<char> span = stackalloc char[propertyName.Length];
        propertyName.CopyTo(span);
        spanPolicy.ConvertSpan(span);
        writer.WritePropertyName(span);
    }
}

