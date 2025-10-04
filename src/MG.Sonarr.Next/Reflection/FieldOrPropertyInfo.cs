using MG.Sonarr.Next.Unions;
using System.Reflection;

namespace MG.Sonarr.Next.Reflection;

/// <summary>
/// An interface that exposes properties about a member.
/// </summary>
public interface IMemberInfo
{
    /// <summary>
    /// Indicates whether the member represented by this instance is static.
    /// </summary>
    bool IsStatic { get; }
    /// <summary>
    /// The name of the member.
    /// </summary>
    string MemberName { get; }
}

/// <summary>
/// An interface that exposes properties and methods for getting/reading the value of a member.
/// </summary>
public interface IMemberGetter : IMemberInfo
{
    /// <summary>
    /// Indicates whether this implementation is able to get the value of the member.
    /// </summary>
    /// <remarks>
    /// It can be expected that when this is <see langword="false"/>, running <see cref="GetValue(object)"/> 
    /// will throw an exception.
    /// </remarks>
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
    /// Indicates whether this implementation is able to set the value of the member.
    /// </summary>
    /// <remarks>
    /// It can be expected that when this is <see langword="false"/>, running <see cref="SetValue(object, object)"/> 
    /// will throw an exception.
    /// </remarks>
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
    private readonly Either<PropertyInfo, FieldInfo> _either;
    private readonly MemberInfo? _member;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly bool _canGet;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly bool _canSet;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly bool _isStatic;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly string? _memberName;

    /// <summary>
    /// Indicates whether this instance has an accessible getter for the member.
    /// </summary>
    /// <remarks>
    /// It can be expected that when this is <see langword="false"/>, running <see cref="GetValue(object)"/> 
    /// will throw an exception.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(_member), nameof(_memberName))]
    public readonly bool CanGet => _canGet;
    /// <summary>
    /// Indicates whether this instance has an accessible getter for the member.
    /// </summary>
    /// <remarks>
    /// It can be expected that when this is <see langword="false"/>, running <see cref="SetValue(object, object)"/> 
    /// will throw an exception.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(_member), nameof(_memberName))]
    public readonly bool CanSet => _canSet;
    /// <summary>
    /// Indicates whether this instance is empty or default-initialized.
    /// </summary>
    [MemberNotNullWhen(false, nameof(_memberName), nameof(_member))]
    public readonly bool IsEmpty => _either.IsDefaultOrEmpty;
    /// <summary>
    /// The name of the field or property represented by this instance.
    /// </summary>
    public readonly string MemberName => _memberName ?? string.Empty;
    /// <summary>
    /// Indicates whether the member represented by this instance is static.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_member), nameof(_memberName))]
    public readonly bool IsStatic => _isStatic;

    /// <summary>
    /// Initializes a new instance of <see cref="FieldOrPropertyInfo"/> with the specified backing <see cref="FieldInfo"/>.
    /// </summary>
    /// <param name="fieldInfo">The field to use.</param>
    /// <exception cref="ArgumentNullException"/>
    public FieldOrPropertyInfo(FieldInfo fieldInfo)
    {
        ArgumentNullException.ThrowIfNull(fieldInfo);
        _memberName = fieldInfo.Name;
        _either = fieldInfo;
        _canGet = true;
        _canSet = !fieldInfo.IsInitOnly;
        _member = fieldInfo;
        _isStatic = fieldInfo.IsStatic;
    }
    /// <summary>
    /// Initializes a new instance of <see cref="FieldOrPropertyInfo"/> with the specified backing <see cref="PropertyInfo"/>.
    /// </summary>
    /// <param name="propertyInfo">The property to use.</param>
    /// <exception cref="ArgumentNullException"/>
    public FieldOrPropertyInfo(PropertyInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);
        _memberName = propertyInfo.Name;
        _either = propertyInfo;
        _canGet = propertyInfo.CanRead;
        _canSet = propertyInfo.CanWrite;
        _member = propertyInfo;
        _isStatic = propertyInfo.GetMethod?.IsStatic ?? propertyInfo.SetMethod?.IsStatic ?? false;
    }

    public readonly object? GetValue(object? instance)
    {
        return _either.MatchObj(instance,
            f1: static (pi, instance) => pi.GetValue(instance),
            f2: static (fi, instance) => fi.GetValue(instance));
    }
    public readonly void SetValue(object? instance, object? value)
    {
        _either.MatchObj(
            f1: static (pi, state) => pi.SetValue(state[0], state[1]),
            f2: static (fi, state) => fi.SetValue(state[0], state[1]),
            instance, value);
    }

    public static implicit operator FieldOrPropertyInfo(FieldInfo fieldInfo)
    {
        return new(fieldInfo);
    }
    public static implicit operator FieldOrPropertyInfo(PropertyInfo propertyInfo)
    {
        return new(propertyInfo);
    }
}