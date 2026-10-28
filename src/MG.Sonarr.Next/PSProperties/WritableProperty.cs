using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Fields;
using System.Collections.Immutable;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties;

public abstract class WritableProperty : PSPropertyInfo
{
	/// <summary>
	/// The largest buffer, in characters, that <see cref="ToString"/> allocates on the stack instead of renting a pooled
	/// array.
	/// </summary>
	private protected const int MAX_STACKALLOC = 256;

	public sealed override bool IsGettable => true;
	public sealed override bool IsSettable => true;
	public sealed override PSMemberTypes MemberType => PSMemberTypes.NoteProperty;
	protected abstract string PSTypeName { get; }

	public virtual ReadOnlyProperty ConvertToReadOnly()
	{
		return new ReadOnlyProperty(this.Name, this.Value);
	}
	protected abstract PSPropertyInfo CopyIntoNew(string name);
	public sealed override PSMemberInfo Copy()
	{
		return this.CopyIntoNew(this.Name);
	}

	protected virtual string? GetValueAsString()
	{
		return this.Value?.ToString();
	}

	/// <exception cref="SetValueException"></exception>
	[DoesNotReturn]
	protected T ThrowNotType<T>()
	{
		throw new SetValueException(
			$"The property '{this.Name}' only can be set with values of type '{this.TypeNameOfValue}'.");
	}
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

	public abstract bool ValueIsProper(object? value);

	static readonly Type _sonarrPropertyType = typeof(WritableSonarrProperty<>);
	private static WritableProperty CreateSonarrObjectProperty(string propertyName, SonarrObject pso)
	{
		object[] ctorArgs = new object[] { propertyName, pso };
		Type[] typeParams = new Type[] { pso.GetType() };

		Type constructedClassType = _sonarrPropertyType.MakeGenericType(typeParams);
		return (WritableProperty?)Activator.CreateInstance(constructedClassType, ctorArgs)
			?? throw new InvalidOperationException("Shit");
	}
}

public abstract class WritableProperty<T> : WritableProperty
{
	protected virtual int MaxValueCharacterLength { get; }
	protected override string PSTypeName => typeof(T).GetPSTypeName(removeBrackets: true);
	public sealed override object? Value
	{
		get => this.ValueAsT;
		set => this.ValueAsT = this.ConvertFromObject(value);
	}
	protected abstract T? ValueAsT { get; set; }

	protected abstract T? ConvertFromObject(object? value);
	public sealed override ReadOnlyProperty ConvertToReadOnly()
	{
		return this.CopyToReadOnly();
	}
	protected abstract ReadOnlyProperty<T> CopyToReadOnly();
	public T? GetValue()
	{
		return this.ValueAsT;
	}
	protected override string? GetValueAsString()
	{
		return this.ValueAsT?.ToString();
	}
	public sealed override string ToString()
	{
		return this.MaxValueCharacterLength <= 0 || this.ValueAsT is not ISpanFormattable spanFormattable
			? base.ToString()
			: ToStringWithSpan(this.PSTypeName, this.Name, spanFormattable, this.MaxValueCharacterLength);
	}
	public override bool ValueIsProper(object? value)
	{
		return value is null || value is T;
	}

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
