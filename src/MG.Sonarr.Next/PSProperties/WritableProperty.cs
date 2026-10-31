using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Fields;
using System.Collections.Immutable;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties;

/// <summary>
/// Represents a note property whose value PowerShell can read and set.
/// </summary>
/// <remarks>
/// <para>PowerShell sees the property as a note property that can be read and set. A derived class decides which
/// values it accepts, and <see cref="ValueIsProper(object?)"/> reports whether it accepts one.</para>
/// <para>Its <see cref="ToString"/> describes the property as <c>&lt;type&gt; &lt;name&gt;=&lt;value&gt;</c>.</para>
/// </remarks>
public abstract class WritableProperty : PSPropertyInfo
{
	/// <summary>
	/// The largest buffer, in characters, that <see cref="ToString"/> allocates on the stack instead of renting a pooled
	/// array.
	/// </summary>
	private protected const int MAX_STACKALLOC = 256;

	/// <summary>
	/// Gets a value that indicates whether the property's value can be read.
	/// </summary>
	/// <value>Always <see langword="true"/>.</value>
	public sealed override bool IsGettable => true;
	/// <summary>
	/// Gets a value that indicates whether the property's value can be set.
	/// </summary>
	/// <value>Always <see langword="true"/>.</value>
	public sealed override bool IsSettable => true;
	/// <summary>
	/// Gets the kind of member that the property is.
	/// </summary>
	/// <value><see cref="PSMemberTypes.NoteProperty"/>.</value>
	public sealed override PSMemberTypes MemberType => PSMemberTypes.NoteProperty;
	/// <summary>
	/// When implemented in a derived class, gets the PowerShell name of the value's type, which
	/// <see cref="ToString"/> shows.
	/// </summary>
	/// <value>The PowerShell name of the value's type, such as <c>int</c>.</value>
	protected abstract string PSTypeName { get; }

	/// <summary>
	/// Creates a read-only property with the property's name and current value.
	/// </summary>
	/// <returns>A new <see cref="ReadOnlyProperty"/> that holds the property's current value.</returns>
	public virtual ReadOnlyProperty ConvertToReadOnly()
	{
		return new ReadOnlyProperty(this.Name, this.Value);
	}
	/// <summary>
	/// When implemented in a derived class, creates a writable property of the same kind that holds the property's
	/// value under the specified name.
	/// </summary>
	/// <param name="name">The name of the new property.</param>
	/// <returns>The new property.</returns>
	protected abstract PSPropertyInfo CopyIntoNew(string name);
	/// <summary>
	/// Creates a copy of the property.
	/// </summary>
	/// <returns>A new property of the same kind, with the same name, that holds the property's value.</returns>
	public sealed override PSMemberInfo Copy()
	{
		return this.CopyIntoNew(this.Name);
	}

	/// <summary>
	/// Gets the text of the property's value, which <see cref="ToString"/> shows.
	/// </summary>
	/// <returns>The value's <see cref="object.ToString"/>, or <see langword="null"/> when the value is null.</returns>
	protected virtual string? GetValueAsString()
	{
		return this.Value?.ToString();
	}

	/// <summary>
	/// Throws an exception that says the property takes only values of its own type.
	/// </summary>
	/// <typeparam name="T">The return type that the caller needs, so that a <see langword="return"/> or a
	/// <see langword="switch"/> arm can call this method.</typeparam>
	/// <returns>This method never returns.</returns>
	/// <exception cref="SetValueException">Thrown always.</exception>
	[DoesNotReturn]
	protected T ThrowNotType<T>()
	{
		throw new SetValueException(
			$"The property '{this.Name}' only can be set with values of type '{this.TypeNameOfValue}'.");
	}
	//BUG: A SortedSet<int> becomes a ReadOnlyTagsProperty, which is always named Tags, so the property loses name when it's anything else.
	/// <summary>
	/// Creates the note property that holds a deserialized value, of the kind that suits the value's type.
	/// </summary>
	/// <remarks>
	/// <para>Strings, numbers, and the supported value types get writable properties. Immutable arrays of fields and
	/// select options, sorted sets of tag IDs and strings, and string-keyed sets of numbers get read-only ones, and any
	/// other value gets a <see cref="PSNoteProperty"/>.</para>
	/// <para>A nested <see cref="SonarrObject"/> gets a writable <see cref="WritableSonarrProperty{T}"/> when
	/// <typeparamref name="TParent"/> is a <see cref="SonarrObject"/> and the nested object's
	/// <see cref="SonarrObject.ShouldBeReadOnly(string, Type)"/> returns <see langword="false"/>, and any other nested
	/// <see cref="PSObject"/> gets a <see cref="ReadOnlyPSObjectProperty{T}"/>.</para>
	/// </remarks>
	/// <typeparam name="TParent">The type of the object that the property is for.</typeparam>
	/// <param name="name">The name of the property.</param>
	/// <param name="value">The property's value.</param>
	/// <returns>The new property.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
	internal static PSPropertyInfo ToProperty<TParent>(string name, object? value) where TParent : PSObject
	{
		ArgumentException.ThrowIfNullOrEmpty(name);

		return value switch
		{
			string strVal => new StringNoteProperty(name, strVal),
			int intVal => new NumberNoteProperty<int>(name, intVal),
			bool boolVal => new StructNoteProperty<bool>(name, boolVal),
			double dubVal => new NumberNoteProperty<double>(name, dubVal),
			long longVal => new NumberNoteProperty<long>(name, longVal),
			decimal decVal => new NumberNoteProperty<decimal>(name, decVal),
			Guid guid => new StructNoteProperty<Guid>(name, guid),
			DateOnly @do => new StructNoteProperty<DateOnly>(name, @do),
			TimeOnly to => new StructNoteProperty<TimeOnly>(name, to),
			DateTimeOffset offset => new StructNoteProperty<DateTimeOffset>(name, offset),
			TimeSpan ts => new StructNoteProperty<TimeSpan>(name, ts),
			PSObject pso => HandlePSObject<TParent>(name, pso),
			ImmutableArray<FieldObject> rof => new ReadOnlyCollectionProperty<FieldObject>(name, rof),
			ImmutableArray<SelectOptionObject> sof => new ReadOnlyCollectionProperty<SelectOptionObject>(name, sof),
			SortedSet<int> iSet => new ReadOnlyTagsProperty(iSet),
			SortedSet<string> sSet => new ReadOnlySetProperty<string>(name, sSet),
			StringKeyValueSet<int> sKvp => new ReadOnlySetProperty<KeyValuePair<string, int>>(name, sKvp),
			_ => new PSNoteProperty(name, value),
		};
	}

	/// <summary>
	/// Creates the note property that holds a nested object.
	/// </summary>
	/// <typeparam name="TParent">The type of the object that the property is for.</typeparam>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="pso">The nested object.</param>
	/// <returns>
	/// A writable <see cref="WritableSonarrProperty{T}"/> when <paramref name="pso"/> is a Sonarr object that may be
	/// writable in a <typeparamref name="TParent"/>; otherwise, a <see cref="ReadOnlyPSObjectProperty{T}"/>.
	/// </returns>
	private static PSPropertyInfo HandlePSObject<TParent>(string propertyName, PSObject pso) where TParent : PSObject
	{
		Type parentType = typeof(TParent);
		if (pso is SonarrObject sonarrPso
			&&
			typeof(SonarrObject).IsAssignableFrom(parentType)
			&&
			!sonarrPso.ShouldBeReadOnly(propertyName, parentType))
		{
			return CreateSonarrObjectProperty(propertyName, sonarrPso);
		}
		else
		{
			return new ReadOnlyPSObjectProperty<PSObject>(propertyName, pso);
		}
	}

	/// <summary>
	/// Returns a description of the property, with its type, name, and value.
	/// </summary>
	/// <returns>The text <c>&lt;type&gt; &lt;name&gt;=&lt;value&gt;</c>, such as <c>string Title=Example</c>.</returns>
	public override string ToString()
	{
		ReadOnlySpan<char> typeName = this.PSTypeName;
		ReadOnlySpan<char> valueStr = this.GetValueAsString();
		ReadOnlySpan<char> name = this.Name;

		int length = typeName.Length + valueStr.Length + name.Length + 2; // Magic number 2 comes from: 1 space and an equals sign.
		using SpanStringBuilder builder = SpanStringBuilder.Create(
			length <= MAX_STACKALLOC
				? stackalloc char[length]
				: length);

		builder.Append(typeName);
		builder.Append(' ');
		builder.Append(name);
		builder.Append('=');
		builder.Append(valueStr);

		return builder.ToString();
	}

	/// <summary>
	/// When implemented in a derived class, determines whether a value can be set on the property.
	/// </summary>
	/// <param name="value">The value to check.</param>
	/// <returns>
	/// <see langword="true"/> if the property accepts <paramref name="value"/>; otherwise, <see langword="false"/>.
	/// </returns>
	public abstract bool ValueIsProper(object? value);

	/// <summary>
	/// The open generic <see cref="WritableSonarrProperty{T}"/> type, which
	/// <see cref="CreateSonarrObjectProperty(string, SonarrObject)"/> closes over a nested object's runtime type.
	/// </summary>
	static readonly Type _sonarrPropertyType = typeof(WritableSonarrProperty<>);
	/// <summary>
	/// Creates a writable property, typed by the nested object's runtime type, that holds a nested Sonarr object.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="pso">The nested object.</param>
	/// <returns>A new <see cref="WritableSonarrProperty{T}"/> closed over the runtime type of <paramref name="pso"/>.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the property can't be created.</exception>
	private static WritableProperty CreateSonarrObjectProperty(string propertyName, SonarrObject pso)
	{
		object[] ctorArgs = new object[] { propertyName, pso };
		Type[] typeParams = new Type[] { pso.GetType() };

		Type constructedClassType = _sonarrPropertyType.MakeGenericType(typeParams);
		return (WritableProperty?)Activator.CreateInstance(constructedClassType, ctorArgs)
			?? throw new InvalidOperationException("Shit");
	}
}

/// <summary>
/// Represents a writable note property whose value is a <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// A derived class holds the value, returns and stores it through <see cref="ValueAsT"/>, and turns a value that is
/// being set into a <typeparamref name="T"/> in <see cref="ConvertFromObject(object?)"/>.
/// </remarks>
/// <typeparam name="T">The type of the property's value.</typeparam>
public abstract class WritableProperty<T> : WritableProperty
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
	protected override string PSTypeName => typeof(T).GetPSTypeName(removeBrackets: true);
	/// <summary>
	/// Gets or sets the property's value.
	/// </summary>
	/// <exception cref="SetValueException">Thrown when the value is set to one that the property doesn't accept.</exception>
	/// <value>The value of <see cref="ValueAsT"/>.</value>
	public sealed override object? Value
	{
		get => this.ValueAsT;
		set => this.ValueAsT = this.ConvertFromObject(value);
	}
	/// <summary>
	/// When implemented in a derived class, gets or sets the property's value as a <typeparamref name="T"/>.
	/// </summary>
	/// <value>The property's value.</value>
	protected abstract T? ValueAsT { get; set; }

	/// <summary>
	/// When implemented in a derived class, converts a value that is being set on the property to a
	/// <typeparamref name="T"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>The value to store.</returns>
	/// <exception cref="SetValueException">Thrown when the property doesn't accept <paramref name="value"/>.</exception>
	protected abstract T? ConvertFromObject(object? value);
	/// <summary>
	/// Creates a read-only property of the matching kind with the property's name and current value.
	/// </summary>
	/// <returns>The result of <see cref="CopyToReadOnly"/>.</returns>
	public sealed override ReadOnlyProperty ConvertToReadOnly()
	{
		return this.CopyToReadOnly();
	}
	/// <summary>
	/// When implemented in a derived class, creates a read-only property of the matching kind with the property's name
	/// and current value.
	/// </summary>
	/// <returns>The new read-only property.</returns>
	protected abstract ReadOnlyProperty<T> CopyToReadOnly();
	/// <summary>
	/// Gets the property's value as a <typeparamref name="T"/>.
	/// </summary>
	/// <returns>The property's current value.</returns>
	public T? GetValue()
	{
		return this.ValueAsT;
	}
	/// <summary>
	/// Gets the text of the property's value, which <see cref="ToString"/> shows.
	/// </summary>
	/// <returns>The value's <see cref="object.ToString"/>, or <see langword="null"/> when the value is null.</returns>
	protected override string? GetValueAsString()
	{
		return this.ValueAsT?.ToString();
	}
	/// <summary>
	/// Returns a description of the property, with its type, name, and value.
	/// </summary>
	/// <returns>The text <c>&lt;type&gt; &lt;name&gt;=&lt;value&gt;</c>, such as <c>int Priority=25</c>.</returns>
	public sealed override string ToString()
	{
		return this.MaxValueCharacterLength <= 0 || this.ValueAsT is not ISpanFormattable spanFormattable
			? base.ToString()
			: ToStringWithSpan(this.PSTypeName, this.Name, spanFormattable, this.MaxValueCharacterLength);
	}
	/// <summary>
	/// Determines whether a value can be set on the property.
	/// </summary>
	/// <param name="value">The value to check.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="value"/> is <see langword="null"/> or a <typeparamref name="T"/>;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	public override bool ValueIsProper(object? value)
	{
		return value is null || value is T;
	}

	/// <summary>
	/// Builds the description of a property whose value formats into a span.
	/// </summary>
	/// <param name="typeName">The PowerShell name of the value's type.</param>
	/// <param name="name">The property's name.</param>
	/// <param name="formattable">The property's value.</param>
	/// <param name="maxLength">The most characters that <paramref name="formattable"/> formats to.</param>
	/// <returns>The text <c>&lt;type&gt; &lt;name&gt;=&lt;value&gt;</c>.</returns>
	private static string ToStringWithSpan(ReadOnlySpan<char> typeName, ReadOnlySpan<char> name, ISpanFormattable formattable, int maxLength)
	{
		int length = 2 + typeName.Length + name.Length + maxLength;
		using SpanStringBuilder builder = SpanStringBuilder.Create(
			length <= MAX_STACKALLOC
				? stackalloc char[length]
				: length);

		builder.Append(typeName);
		builder.Append(' ');
		builder.Append(name);
		builder.Append('=');
		builder.Append(formattable, maxLength, format: default, Statics.DefaultProvider);

		return builder.ToString();
	}
}
