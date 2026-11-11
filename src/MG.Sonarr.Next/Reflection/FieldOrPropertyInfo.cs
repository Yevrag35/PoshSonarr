using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Reflection;

/// <summary>
/// An interface that exposes properties about a member.
/// </summary>
public interface IMemberInfo
{
	/// <summary>
	/// Gets the name of the member.
	/// </summary>
	/// <value>The member name.</value>
	string MemberName { get; }
}

/// <summary>
/// An interface that exposes properties and methods for getting/reading the value of a member.
/// </summary>
public interface IMemberGetter : IMemberInfo
{
	/// <summary>
	/// Gets a value that indicates whether this implementation can get the value of the member.
	/// </summary>
	/// <remarks>
	/// When this value is <see langword="false"/>, calling <see cref="GetValue(object)"/> can throw.
	/// </remarks>
	/// <value><see langword="true"/> when value reads are supported; otherwise, <see langword="false"/>.</value>
	bool CanGet { get; }

	/// <summary>
	/// Gets the value of the member represented by this instance on the specified object.
	/// </summary>
	/// <param name="instance">The instance to get the value from -or- <see langword="null"/> if the member is static.</param>
	/// <returns>The value of the member on the specified object.</returns>
	/// <inheritdoc cref="FieldInfo.GetValue(object)" path="/exception"/>
	/// <inheritdoc cref="PropertyInfo.GetValue(object)" path="/exception"/>
	object? GetValue(object? instance);
}
/// <summary>
/// An interface that exposes properties and methods for setting/writing the value of a member.
/// </summary>
public interface IMemberSetter : IMemberInfo
{
	/// <summary>
	/// Gets a value that indicates whether this implementation can set the value of the member.
	/// </summary>
	/// <remarks>
	/// When this value is <see langword="false"/>, calling <see cref="SetValue(object, object)"/> can throw.
	/// </remarks>
	/// <value><see langword="true"/> when value writes are supported; otherwise, <see langword="false"/>.</value>
	bool CanSet { get; }

	/// <summary>
	/// Sets the value of the member represented by this instance on the specified object.
	/// </summary>
	/// <param name="instance">The instance to set the value on -or- <see langword="null"/> if the member is static.</param>
	/// <param name="value">The value to set.</param>
	/// <inheritdoc cref="PropertyInfo.SetValue(object, object)" path="/exception"/>
	/// <inheritdoc cref="FieldInfo.SetValue(object, object)" path="/exception"/>
	void SetValue(object? instance, object? value);
}

/// <summary>
/// Represents a field or property and provides methods for getting and setting the value of either type of member.
/// </summary>
[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay(@"\{{MemberName,nq} = {_either}\}")]
public readonly struct FieldOrPropertyInfo : IMemberGetter, IMemberSetter, IMemberInfo
{
	private readonly ObjEither<PropertyInfo, FieldInfo> _either;
	private readonly MemberInfo? _member;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly bool _canGet;
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly bool _canSet;
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly string? _memberName;

	/// <summary>
	/// Gets a value that indicates whether this instance has an accessible getter for the member.
	/// </summary>
	/// <remarks>
	/// When this value is <see langword="false"/>, calling <see cref="GetValue(object)"/> can throw.
	/// </remarks>
	/// <value><see langword="true"/> when value reads are supported; otherwise, <see langword="false"/>.</value>
	[MemberNotNullWhen(true, nameof(_member), nameof(_memberName))]
	public readonly bool CanGet => _canGet;

	/// <summary>
	/// Gets a value that indicates whether this instance has an accessible setter for the member.
	/// </summary>
	/// <remarks>
	/// It's <see langword="false"/> for a property without a setter, a read-only field, and a constant, none of which
	/// <see cref="SetValue(object, object)"/> should be called for.
	/// </remarks>
	/// <value><see langword="true"/> when value writes are supported; otherwise, <see langword="false"/>.</value>
	[MemberNotNullWhen(true, nameof(_member), nameof(_memberName))]
	public readonly bool CanSet => _canSet;

	/// <summary>
	/// Gets a value that indicates whether this instance is empty or default-initialized.
	/// </summary>
	/// <value><see langword="true"/> when this instance is empty; otherwise, <see langword="false"/>.</value>
	[MemberNotNullWhen(false, nameof(_memberName), nameof(_member))]
	public readonly bool IsEmpty => _either.IsNeither;

	/// <summary>
	/// Gets the name of the field or property represented by this instance.
	/// </summary>
	/// <value>The field or property name, or an empty string when this instance is empty.</value>
	public readonly string MemberName => _memberName ?? string.Empty;

	/// <summary>
	/// Initializes a new instance of <see cref="FieldOrPropertyInfo"/> with the specified backing <see cref="FieldInfo"/>.
	/// </summary>
	/// <param name="fieldInfo">The field to use.</param>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="fieldInfo"/> is null.</exception>
	public FieldOrPropertyInfo(FieldInfo fieldInfo)
	{
		ArgumentNullException.ThrowIfNull(fieldInfo);
		_memberName = fieldInfo.Name;
		_either = fieldInfo;
		_canGet = true;
		_canSet = !fieldInfo.IsInitOnly && !fieldInfo.IsLiteral;
		_member = fieldInfo;
	}
	/// <summary>
	/// Initializes a new instance of <see cref="FieldOrPropertyInfo"/> with the specified backing <see cref="PropertyInfo"/>.
	/// </summary>
	/// <param name="propertyInfo">The property to use.</param>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="propertyInfo"/> is null.</exception>
	public FieldOrPropertyInfo(PropertyInfo propertyInfo)
	{
		ArgumentNullException.ThrowIfNull(propertyInfo);
		_memberName = propertyInfo.Name;
		_either = propertyInfo;
		_canGet = propertyInfo.CanRead;
		_canSet = propertyInfo.CanWrite;
		_member = propertyInfo;
	}

	/// <summary>
	/// Gets the value of the represented member from the specified instance.
	/// </summary>
	/// <param name="instance">The instance to read from, or null for static members.</param>
	/// <returns>The member value from <paramref name="instance"/>.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the member does not have a getter.</exception>
	public readonly object? GetValue(object? instance)
	{
		if (!_canGet) ThrowCannotGetValue(_memberName);

		unsafe
		{
			// SAFETY: Both pointers are addresses of static local functions, which the compiler matched to the pointer
			// types.
			return _either.FnMatch(instance,
				f1: new FnPtr<object?, PropertyInfo, object?>(&getFromT1),
				f2: new FnPtr<object?, FieldInfo, object?>(&getFromT2));
		}

		static object? getFromT1(object? instance, PropertyInfo pi) => pi.GetValue(instance);
		static object? getFromT2(object? instance, FieldInfo fi) => fi.GetValue(instance);
	}

	/// <summary>
	/// Sets the value of the represented member on the specified instance.
	/// </summary>
	/// <param name="instance">The instance to write to, or null for static members.</param>
	/// <param name="value">The value to assign.</param>
	/// <inheritdoc cref="PropertyInfo.SetValue(object, object)" path="/exception"/>
	/// <inheritdoc cref="FieldInfo.SetValue(object, object)" path="/exception"/>
	public readonly void SetValue(object? instance, object? value)
	{
		_either.Match(
			state: (instance, value),
			f1: static (state, pi) => pi.SetValue(state.instance, state.value),
			f2: static (state, fi) => fi.SetValue(state.instance, state.value));
	}

	/// <summary>
	/// Throws an <see cref="InvalidOperationException"/> indicating that the value of the member cannot be retrieved because it does not have a getter.
	/// </summary>
	/// <param name="memberName">The name of the member that cannot be accessed.</param>
	/// <exception cref="InvalidOperationException"/>
	[DoesNotReturn, StackTraceHidden, DebuggerStepThrough]
	private static void ThrowCannotGetValue(string? memberName)
	{
		throw new InvalidOperationException($"Cannot get value from member '{memberName}' because it does not have a getter.");
	}

	/// <summary>
	/// Implicitly converts a <see cref="FieldInfo"/> value to a <see cref="FieldOrPropertyInfo"/> instance.
	/// </summary>
	/// <param name="fieldInfo">The field metadata to wrap.</param>
	/// <returns>A wrapper for <paramref name="fieldInfo"/>.</returns>
	public static implicit operator FieldOrPropertyInfo(FieldInfo fieldInfo)
	{
		return new(fieldInfo);
	}

	/// <summary>
	/// Implicitly converts a <see cref="PropertyInfo"/> value to a <see cref="FieldOrPropertyInfo"/> instance.
	/// </summary>
	/// <param name="propertyInfo">The property metadata to wrap.</param>
	/// <returns>A wrapper for <paramref name="propertyInfo"/>.</returns>
	public static implicit operator FieldOrPropertyInfo(PropertyInfo propertyInfo)
	{
		return new(propertyInfo);
	}
}