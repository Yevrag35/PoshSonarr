using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Extensions.Reflection;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties;

/// <summary>
/// Represents a note property whose value is set once, when the property is created, and that PowerShell can't
/// change.
/// </summary>
/// <remarks>
/// <para>PowerShell sees the property as a note property that can be read but not set.</para>
/// <para>The property can't be given another value, though a mutable value, such as a collection, can still change
/// itself.</para>
/// </remarks>
public class ReadOnlyProperty : PSPropertyInfo
{
	/// <summary>
	/// The type name that <see cref="TypeNameOfValue"/> returns for a null value.
	/// </summary>
	private const string DEFAULT_TYPE = "System.Object";
	/// <summary>
	/// The value that the constructor set.
	/// </summary>
	private readonly object? _value;

	/// <summary>
	/// Gets the kind of member that the property is.
	/// </summary>
	/// <value><see cref="PSMemberTypes.NoteProperty"/>.</value>
	public sealed override PSMemberTypes MemberType => PSMemberTypes.NoteProperty;
	/// <summary>
	/// Gets a value that indicates whether the property's value can be read.
	/// </summary>
	/// <value>Always <see langword="true"/>.</value>
	public sealed override bool IsGettable => true;
	/// <summary>
	/// Gets a value that indicates whether the property's value can be set.
	/// </summary>
	/// <value>Always <see langword="false"/>.</value>
	public sealed override bool IsSettable => false;
	/// <summary>
	/// Gets the full name of the property's value type.
	/// </summary>
	/// <value>The full name of the value's runtime type, or <c>System.Object</c> when the value is null.</value>
	public override string TypeNameOfValue => this.Value?.GetType().GetName() ?? DEFAULT_TYPE;
	/// <summary>
	/// Gets the property's value.
	/// </summary>
	/// <remarks>
	/// The property's setter exists because <see cref="PSPropertyInfo"/> declares one, and it always throws.
	/// </remarks>
	/// <exception cref="SetValueException">
	/// Thrown when the value is set. Its inner exception is a <see cref="ReadOnlyPropertyException"/>.
	/// </exception>
	/// <value>The value that the property was created with.</value>
	public override object? Value
	{
		get => _value;
		set
		{
			ReadOnlyPropertyException ex = new(this.Name);
			throw new SetValueException(ex.Message, ex);
		}
	}

	/// <summary>
	/// Initializes a new <see cref="ReadOnlyProperty"/> instance with the specified name, for a derived class that
	/// holds its own value.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is empty.</exception>
	protected ReadOnlyProperty(string propertyName)
	{
		ArgumentException.ThrowIfNullOrEmpty(propertyName);
		this.SetMemberName(propertyName);
	}
	/// <summary>
	/// Initializes a new <see cref="ReadOnlyProperty"/> instance with the specified name and value.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="value">The property's value.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is empty.</exception>
	public ReadOnlyProperty(string propertyName, object? value)
		: this(propertyName)
	{
		_value = value;
	}

	/// <summary>
	/// Creates a copy of the property.
	/// </summary>
	/// <remarks>
	/// A value that implements <see cref="ICloneable"/> is cloned for the copy, and any other value is shared with
	/// it.
	/// </remarks>
	/// <returns>A new <see cref="ReadOnlyProperty"/> with the same name and the value or its clone.</returns>
	public override PSMemberInfo Copy()
	{
		object? addValue = _value;
		if (_value is ICloneable clonable)
		{
			addValue = clonable.Clone();
		}

		return new ReadOnlyProperty(this.Name, addValue);
	}
}

/// <summary>
/// Represents a read-only note property whose value is a <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// A derived class holds the value and returns it from <see cref="ValueAsT"/>, and its
/// <see cref="ToString"/> describes the property as <c>read-only &lt;type&gt; &lt;name&gt;=&lt;value&gt;</c>.
/// </remarks>
/// <typeparam name="T">The type of the property's value.</typeparam>
public abstract class ReadOnlyProperty<T> : ReadOnlyProperty
{
	/// <summary>
	/// Gets the most characters that the property's value formats to.
	/// </summary>
	/// <remarks>
	/// When this is greater than 0 and the value is an <see cref="ISpanFormattable"/>, <see cref="ToString"/> formats
	/// the value into a buffer of this many characters instead of calling the value's <see cref="object.ToString"/>.
	/// </remarks>
	/// <value>The longest the value's text can be, or 0, the default, to format it through
	/// <see cref="object.ToString"/>.</value>
	protected virtual int MaxValueCharacterLength { get; }
	/// <summary>
	/// Gets the PowerShell name of the value's type, which <see cref="ToString"/> shows.
	/// </summary>
	/// <value>The PowerShell name of <typeparamref name="T"/> without its enclosing brackets, such as <c>int</c>.</value>
	protected virtual string PSTypeName => typeof(T).GetPSTypeName(removeBrackets: true);
	/// <summary>
	/// Gets the full name of the property's value type.
	/// </summary>
	/// <value>The full name of <typeparamref name="T"/>.</value>
	public override string TypeNameOfValue => typeof(T).GetName();

	/// <summary>
	/// Gets the property's value.
	/// </summary>
	/// <remarks>
	/// The property's setter exists because <see cref="PSPropertyInfo"/> declares one, and it always throws.
	/// </remarks>
	/// <exception cref="SetValueException">
	/// Thrown when the value is set. Its inner exception is a <see cref="ReadOnlyPropertyException"/>.
	/// </exception>
	/// <value>The value of <see cref="ValueAsT"/>.</value>
	public sealed override object? Value
	{
		get => this.ValueAsT;
		set => base.Value = value;  // this errors
	}

	/// <summary>
	/// When implemented in a derived class, gets the property's value as a <typeparamref name="T"/>.
	/// </summary>
	/// <value>The property's value.</value>
	protected abstract T ValueAsT { get; }

	/// <summary>
	/// Initializes a new <see cref="ReadOnlyProperty{T}"/> instance with the specified name.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is empty.</exception>
	protected ReadOnlyProperty(string propertyName)
		: base(propertyName)
	{
	}

	/// <summary>
	/// Copies the text of the property's value, without leading or trailing white space, into a span.
	/// </summary>
	/// <param name="span">The span to write the text into.</param>
	/// <param name="written">
	/// When this method returns, the length of the trimmed text, whether or not it fit in <paramref name="span"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the text fit in <paramref name="span"/>; otherwise, <see langword="false"/>.
	/// </returns>
	protected virtual bool TryWriteValueToSpan(Span<char> span, out int written)
	{
		ReadOnlySpan<char> valueAsString = this.ValueAsT?.ToString();
		valueAsString = valueAsString.Trim();

		written = valueAsString.Length;
		return valueAsString.TryCopyTo(span);
	}

	/// <summary>
	/// Throws an exception that says the property takes only values of its own type.
	/// </summary>
	/// <returns>This method never returns.</returns>
	/// <exception cref="SetValueException">Thrown always.</exception>
	[DoesNotReturn]
	protected T ThrowNotType()
	{
		throw new SetValueException(
			$"The property '{this.Name}' only can be set with values of type '{this.TypeNameOfValue}'.");
	}

	/// <summary>
	/// The largest buffer, in characters, that <see cref="ToString"/> allocates on the stack instead of renting a pooled
	/// array.
	/// </summary>
	private const int MAX_STACKALLOC = 256;
	/// <summary>
	/// The text that starts the result of <see cref="ToString"/>.
	/// </summary>
	private const string READ_ONLY = "read-only ";
	/// <summary>
	/// Returns a description of the property, with its type, name, and value.
	/// </summary>
	/// <returns>The text <c>read-only &lt;type&gt; &lt;name&gt;=&lt;value&gt;</c>, such as <c>read-only int Id=5</c>.</returns>
	public override string ToString()
	{
		ReadOnlySpan<char> typeName = this.PSTypeName;
		ReadOnlySpan<char> name = this.Name;
		if (this.MaxValueCharacterLength <= 0 || this.ValueAsT is not ISpanFormattable spanFormattable)
		{
			return ToStringNoSpan(typeName, this.ValueAsT, name);
		}
		else
		{
			return ToStringWithSpan(typeName, name, spanFormattable, this.MaxValueCharacterLength);
		}
	}

	/// <summary>
	/// Builds the description of a property whose value formats through <see cref="object.ToString"/>.
	/// </summary>
	/// <remarks>
	/// The value's text is trimmed of leading and trailing white space, and a null value shows as empty.
	/// </remarks>
	/// <param name="typeName">The PowerShell name of the value's type.</param>
	/// <param name="valueAsT">The property's value.</param>
	/// <param name="name">The property's name.</param>
	/// <returns>The text <c>read-only &lt;type&gt; &lt;name&gt;=&lt;value&gt;</c>.</returns>
	private static string ToStringNoSpan(ReadOnlySpan<char> typeName, T? valueAsT, ReadOnlySpan<char> name)
	{
		ReadOnlySpan<char> valueStr = valueAsT?.ToString();
		valueStr = valueStr.Trim();

		int length = READ_ONLY.Length + typeName.Length + valueStr.Length + name.Length + 2; // Magic number 2 comes from: 1 space and an equals sign.
		using SpanStringBuilder builder = SpanStringBuilder.Create(
			length <= MAX_STACKALLOC
				? stackalloc char[length]
				: length);

		builder.Append(READ_ONLY);
		builder.Append(typeName);
		builder.Append(' ');
		builder.Append(name);
		builder.Append('=');
		builder.Append(valueStr);

		return builder.ToString();
	}
	/// <summary>
	/// Builds the description of a property whose value formats into a span.
	/// </summary>
	/// <param name="typeName">The PowerShell name of the value's type.</param>
	/// <param name="name">The property's name.</param>
	/// <param name="formattable">The property's value.</param>
	/// <param name="maxLength">The most characters that <paramref name="formattable"/> formats to.</param>
	/// <returns>The text <c>read-only &lt;type&gt; &lt;name&gt;=&lt;value&gt;</c>.</returns>
	private static string ToStringWithSpan(ReadOnlySpan<char> typeName, ReadOnlySpan<char> name, ISpanFormattable formattable, int maxLength)
	{
		int length = 2 + READ_ONLY.Length + typeName.Length + name.Length + maxLength;
		using SpanStringBuilder builder = SpanStringBuilder.Create(
			length <= MAX_STACKALLOC
				? stackalloc char[length]
				: length);

		builder.Append(READ_ONLY);
		builder.Append(typeName);
		builder.Append(' ');
		builder.Append(name);
		builder.Append('=');
		builder.Append(formattable, maxLength, format: default, Statics.DefaultProvider);

		return builder.ToString();
	}
}
