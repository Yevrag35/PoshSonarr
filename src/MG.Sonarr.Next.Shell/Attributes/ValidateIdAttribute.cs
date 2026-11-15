using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Unions;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Shell.Attributes;

/// <summary>
/// Provides the validation of the ID that a parameter's argument gives against a <see cref="ValidateRangeKind"/>.
/// </summary>
/// <remarks>
/// <para>
/// The attribute reads the ID from the argument itself, by the rules that the constructor's remarks give, so it suits a
/// parameter that takes an <see cref="IHasId"/>, a <see cref="PSObject"/> with an <c>Id</c> property, an
/// <see cref="int"/>, or an <see cref="Either{T1, T2}"/> that holds one of them. An argument that gives no ID, a
/// <see langword="null"/> one included, is handled as <see cref="NullBehavior"/> says.
/// </para>
/// <para>
/// A parameter typed as a pipeable interface uses <see cref="ValidateIdAttribute{T}"/> instead, which reads the ID
/// through that interface.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ValidateIdAttribute : ValidateArgumentsAttribute
{
	private readonly IdValidator _predicate;

	/// <summary>
	/// Gets the range that the ID must be in to pass validation.
	/// </summary>
	/// <value>The range that the ID is checked against.</value>
	public ValidateRangeKind Kind { get; }
	/// <summary>
	/// Gets or sets what happens when the argument gives no ID.
	/// </summary>
	/// <remarks>
	/// An argument gives no ID when it's <see langword="null"/>, when it's a string, or when it's of a type that the
	/// attribute doesn't read an ID from, such as a <see cref="PSObject"/> without an <see cref="int"/> <c>Id</c>
	/// property.
	/// </remarks>
	/// <value>
	/// The behavior for a missing ID. The default, <see cref="InputNullBehavior.EnforceNotNull"/>, fails validation.
	/// </value>
	public InputNullBehavior NullBehavior { get; init; }

	/// <summary>
	/// Initializes a new <see cref="ValidateIdAttribute"/> instance that reads the ID from the argument itself.
	/// </summary>
	/// <remarks>
	/// The argument gives the <see cref="IHasId.Id"/> of an <see cref="IHasId"/>, the <see cref="int"/> that an
	/// <see cref="Either{T1, T2}"/> of <see cref="int"/> and <see cref="string"/> holds, the <c>Id</c> property of a
	/// <see cref="PSObject"/>, or an <see cref="int"/> itself, checked in that order. An <see cref="Either{T1, T2}"/> of
	/// <see cref="int"/> and a reference type gives the <see cref="int"/> that it holds, or else the ID that its object
	/// gives by the same rules. Any other argument gives no ID, and <see cref="NullBehavior"/> decides what happens to
	/// it.
	/// </remarks>
	/// <param name="kind">The predefined range to validate the ID against.</param>
	public ValidateIdAttribute(ValidateRangeKind kind)
	{
		this.Kind = kind;
		_predicate = IdValidationHelper.GetIdValidator(kind);
	}

	/// <summary>
	/// Reads the ID that the argument gives and checks it against <see cref="Kind"/>.
	/// </summary>
	/// <param name="arguments">The argument, which can be <see langword="null"/>.</param>
	/// <param name="engineIntrinsics">The engine APIs of the session that binds the argument, which the method doesn't
	/// read.</param>
	/// <exception cref="ValidationMetadataException">Thrown when the ID is out of range, or when
	/// <see cref="NullBehavior"/> refuses an argument that gives no ID, or one that gives an ID.</exception>
	protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
	{
		IdValidationHelper.DoValidation(argument: arguments, this.NullBehavior, _predicate);
	}
}

/// <summary>
/// Provides the validation of the ID that a parameter's argument gives as a <typeparamref name="T"/> against a
/// <see cref="ValidateRangeKind"/>.
/// </summary>
/// <remarks>
/// The ID is the one that the argument's <see cref="IPipeable{T}.GetId"/> returns for <typeparamref name="T"/>, so an
/// object that implements several pipeable interfaces gives the ID that the parameter's interface exposes. A
/// <see langword="null"/> argument gives no ID, and <see cref="NullBehavior"/> decides what happens to it and to a
/// <see langword="null"/> ID. An argument that isn't a <typeparamref name="T"/> fails validation.
/// </remarks>
/// <typeparam name="T">The pipeable interface that the parameter's type names.</typeparam>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ValidateIdAttribute<T> : ValidateArgumentsAttribute where T : IPipeable<T>
{
	private readonly IdValidator _predicate;

	/// <summary>
	/// Gets the range that the ID must be in to pass validation.
	/// </summary>
	/// <value>The range that the ID is checked against.</value>
	public ValidateRangeKind Kind { get; }
	/// <summary>
	/// Gets or sets what happens when the argument is <see langword="null"/> or gives a <see langword="null"/> ID.
	/// </summary>
	/// <value>
	/// The behavior for a missing ID. The default, <see cref="InputNullBehavior.EnforceNotNull"/>, fails validation.
	/// </value>
	public InputNullBehavior NullBehavior { get; init; }

	/// <summary>
	/// Initializes a new <see cref="ValidateIdAttribute{T}"/> instance that checks the argument's ID against a range.
	/// </summary>
	/// <param name="kind">The predefined range to validate the ID against.</param>
	public ValidateIdAttribute(ValidateRangeKind kind)
	{
		this.Kind = kind;
		_predicate = IdValidationHelper.GetIdValidator(kind);
	}

	/// <summary>
	/// Reads the ID that the argument gives as a <typeparamref name="T"/> and checks it against <see cref="Kind"/>.
	/// </summary>
	/// <param name="arguments">The argument, which can be <see langword="null"/>.</param>
	/// <param name="engineIntrinsics">The engine APIs of the session that binds the argument, which the method doesn't
	/// read.</param>
	/// <exception cref="ValidationMetadataException">Thrown when the argument isn't a <typeparamref name="T"/>, when the
	/// ID is out of range, or when <see cref="NullBehavior"/> refuses a missing ID or an ID that is present.</exception>
	protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
	{
		IdValidationHelper.DoValidation(IdValidationHelper.GetIdFromPipeable<T>(arguments), this.NullBehavior, _predicate);
	}
}
/// <summary>
/// Provides the validation of the ID that each element of a parameter's list argument gives against a
/// <see cref="ValidateRangeKind"/>.
/// </summary>
/// <remarks>
/// <para>
/// The attribute reads each element's ID from the element itself, by the rules that the constructor's remarks give, so
/// it suits an array parameter whose elements are <see cref="IHasId"/> objects, <see cref="PSObject"/> objects with an
/// <c>Id</c> property, <see cref="int"/> values, or <see cref="Either{T1, T2}"/> values that hold one of them. An
/// element that gives no ID, a <see langword="null"/> one included, is handled as <see cref="NullBehavior"/> says, so
/// with <see cref="InputNullBehavior.Ignore"/>, the names in an <see cref="Either{T1, T2}"/> of <see cref="string"/>
/// and <see cref="int"/> pass and only the IDs are checked.
/// </para>
/// <para>
/// A parameter whose elements are a pipeable interface uses <see cref="ValidateIdsAttribute{T}"/> instead, which reads
/// each ID through that interface.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ValidateIdsAttribute : ValidateEnumeratedArgumentsAttribute
{
	private readonly IdValidator _predicate;
	/// <summary>
	/// Gets the range that each element's ID must be in to pass validation.
	/// </summary>
	/// <value>The range that the IDs are checked against.</value>
	public ValidateRangeKind Kind { get; }
	/// <summary>
	/// Gets or sets what happens when an element gives no ID.
	/// </summary>
	/// <remarks>
	/// An element gives no ID when it's <see langword="null"/>, when it's a string, or when it's of a type that the
	/// attribute doesn't read an ID from, such as a <see cref="PSObject"/> without an <see cref="int"/> <c>Id</c>
	/// property. The behavior applies to each element on its own.
	/// </remarks>
	/// <value>
	/// The behavior for a missing ID. The default, <see cref="InputNullBehavior.EnforceNotNull"/>, fails validation.
	/// </value>
	public InputNullBehavior NullBehavior { get; init; }

	/// <summary>
	/// Initializes a new <see cref="ValidateIdsAttribute"/> instance that reads each element's ID from the element itself.
	/// </summary>
	/// <remarks>
	/// An element gives the <see cref="IHasId.Id"/> of an <see cref="IHasId"/>, the <see cref="int"/> that an
	/// <see cref="Either{T1, T2}"/> of <see cref="int"/> and <see cref="string"/> holds, the <c>Id</c> property of a
	/// <see cref="PSObject"/>, or an <see cref="int"/> itself, checked in that order. An <see cref="Either{T1, T2}"/> of
	/// <see cref="int"/> and a reference type gives the <see cref="int"/> that it holds, or else the ID that its object
	/// gives by the same rules. Any other element gives no ID, and <see cref="NullBehavior"/> decides what happens to
	/// it.
	/// </remarks>
	/// <param name="kind">The predefined range to validate the element's ID against.</param>
	public ValidateIdsAttribute(ValidateRangeKind kind)
	{
		this.Kind = kind;
		_predicate = IdValidationHelper.GetIdValidator(kind);
	}

	/// <summary>
	/// Reads the ID that an element of the argument gives and checks it against <see cref="Kind"/>.
	/// </summary>
	/// <param name="element">The element, which can be <see langword="null"/>.</param>
	/// <exception cref="ValidationMetadataException">Thrown when the ID is out of range, or when
	/// <see cref="NullBehavior"/> refuses an element that gives no ID, or one that gives an ID.</exception>
	protected override void ValidateElement(object element)
	{
		IdValidationHelper.DoValidation(argument: element, this.NullBehavior, _predicate);
	}
}

/// <summary>
/// Provides the validation of the ID that each element of a parameter's argument gives as a <typeparamref name="T"/>
/// against a <see cref="ValidateRangeKind"/>.
/// </summary>
/// <remarks>
/// The ID is the one that the element's <see cref="IPipeable{T}.GetId"/> returns for <typeparamref name="T"/>, so an
/// object that implements several pipeable interfaces gives the ID that the parameter's interface exposes. A
/// <see langword="null"/> element gives no ID, and <see cref="NullBehavior"/> decides what happens to it and to a
/// <see langword="null"/> ID. An element that isn't a <typeparamref name="T"/> fails validation.
/// </remarks>
/// <typeparam name="T">The pipeable interface that the parameter's element type names.</typeparam>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ValidateIdsAttribute<T> : ValidateEnumeratedArgumentsAttribute where T : IPipeable<T>
{
	private readonly IdValidator _predicate;

	/// <summary>
	/// Gets the range that each element's ID must be in to pass validation.
	/// </summary>
	/// <value>The range that the IDs are checked against.</value>
	public ValidateRangeKind Kind { get; }
	/// <summary>
	/// Gets or sets what happens when an element is <see langword="null"/> or gives a <see langword="null"/> ID.
	/// </summary>
	/// <value>
	/// The behavior for a missing ID. The default, <see cref="InputNullBehavior.EnforceNotNull"/>, fails validation.
	/// </value>
	public InputNullBehavior NullBehavior { get; init; }

	/// <summary>
	/// Initializes a new <see cref="ValidateIdsAttribute{T}"/> instance that checks each element's ID against a range.
	/// </summary>
	/// <param name="kind">The predefined range to validate each element's ID against.</param>
	public ValidateIdsAttribute(ValidateRangeKind kind)
	{
		this.Kind = kind;
		_predicate = IdValidationHelper.GetIdValidator(kind);
	}

	/// <summary>
	/// Reads the ID that an element of the argument gives as a <typeparamref name="T"/> and checks it against
	/// <see cref="Kind"/>.
	/// </summary>
	/// <param name="element">The element, which can be <see langword="null"/>.</param>
	/// <exception cref="ValidationMetadataException">Thrown when the element isn't a <typeparamref name="T"/>, when the
	/// ID is out of range, or when <see cref="NullBehavior"/> refuses a missing ID or an ID that is present.</exception>
	protected override void ValidateElement(object element)
	{
		IdValidationHelper.DoValidation(IdValidationHelper.GetIdFromPipeable<T>(element), this.NullBehavior, _predicate);
	}
}

/// <summary>
/// Represents the range check of an ID validation attribute, as a pointer to the static method that checks one
/// <see cref="ValidateRangeKind"/>.
/// </summary>
/// <remarks>
/// The struct holds a managed function pointer rather than a delegate, so an attribute keeps its check without
/// allocating, and the call needs no delegate invocation. It's immutable and thread-safe.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 8)]
internal readonly struct IdValidator
{
	private readonly delegate* managed<int, out ValidateRangeKind, bool> _ptr;

	/// <summary>
	/// Initializes a new <see cref="IdValidator"/> instance with the specified pointer to a range check.
	/// </summary>
	/// <param name="ptr">The pointer to a static method that returns whether an ID is in its range and gives the range
	/// as its <see langword="out"/> argument.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="ptr"/> is null.</exception>
	internal IdValidator(delegate* managed<int, out ValidateRangeKind, bool> ptr)
	{
		ArgumentNullException.ThrowIfNull(ptr);
		_ptr = ptr;
	}

	/// <summary>
	/// Checks whether the specified ID is in the range.
	/// </summary>
	/// <param name="id">The ID to check.</param>
	/// <param name="kind">When the method returns, the range that the ID was checked against, for the error
	/// message.</param>
	/// <returns><see langword="true"/> if <paramref name="id"/> is in the range; otherwise,
	/// <see langword="false"/>.</returns>
	internal bool Invoke(int id, out ValidateRangeKind kind)
	{
		unsafe
		{
			// SAFETY: The constructor turns away a null pointer, and the pointer's type limits it to a method of this
			// signature, which the compiler checks where the pointer is taken. Each attribute builds its check in its
			// constructor, so none holds the null pointer of a default instance.
			return _ptr(id, out kind);
		}
	}
}

/// <summary>
/// Provides the ID validation attributes' shared work: reading the ID that an argument gives, applying an
/// <see cref="InputNullBehavior"/> to a missing ID, and checking an ID against a <see cref="ValidateRangeKind"/>.
/// </summary>
/// <remarks>
/// The attributes without a type argument read an ID from the argument itself, with <see cref="GetId(object?)"/>, and
/// the generic ones through <see cref="IPipeable{T}.GetId"/>, with <see cref="GetIdFromPipeable{T}(object?)"/>. All
/// members are static and thread-safe.
/// </remarks>
file static class IdValidationHelper
{
	/// <summary>
	/// Represents a method that reads the ID from an argument of one runtime type.
	/// </summary>
	/// <param name="element">The argument, or one element of it, which isn't <see langword="null"/>.</param>
	/// <returns>The ID that <paramref name="element"/> gives, or <see langword="null"/> when it gives none.</returns>
	private delegate int? GetIdDelegate(object element);

	/// <summary>
	/// Applies the specified null behavior to an ID that may be missing, and checks an ID that is present, or the one that
	/// stands in for a missing ID, against a range.
	/// </summary>
	/// <remarks>
	/// A missing ID fails under <see cref="InputNullBehavior.EnforceNotNull"/>, passes unchecked under
	/// <see cref="InputNullBehavior.Ignore"/> and <see cref="InputNullBehavior.EnforceNull"/>, and is checked as 0, 1, or
	/// -1 under the <c>PassAs</c> behaviors. An ID that is present fails under <see cref="InputNullBehavior.EnforceNull"/>.
	/// An undefined behavior lets a missing ID pass.
	/// </remarks>
	/// <param name="possibleId">The ID, or <see langword="null"/> when the argument gives none.</param>
	/// <param name="nullBehavior">What happens when <paramref name="possibleId"/> is <see langword="null"/>.</param>
	/// <param name="predicate">The range that the ID must be in.</param>
	/// <exception cref="ValidationMetadataException">Thrown when the ID is out of range, or when
	/// <paramref name="nullBehavior"/> refuses a missing ID or an ID that is present.</exception>
	internal static void DoValidation(int? possibleId, InputNullBehavior nullBehavior, IdValidator predicate)
	{
		if (!possibleId.HasValue)
		{
			switch (nullBehavior)
			{
				case InputNullBehavior.EnforceNotNull:
					ThrowIsNull(possibleId);
					return;

				case InputNullBehavior.Ignore:
				case InputNullBehavior.EnforceNull:
					goto default;

				case InputNullBehavior.PassAsZero:
					possibleId = 0;
					break;

				case InputNullBehavior.PassAsOne:
					possibleId = 1;
					break;

				case InputNullBehavior.PassAsNegativeOne:
					possibleId = -1;
					break;

				default:
					return;
			}
		}
		else if (nullBehavior == InputNullBehavior.EnforceNull)
		{
			ThrowIsNotNull(possibleId);
		}

		ValidateId(possibleId.Value, predicate);
	}

	/// <summary>
	/// Validates the ID that an argument gives when the attribute has no pipeable type to read it through.
	/// </summary>
	/// <remarks>
	/// <see cref="GetId(object?)"/> reads the ID. A model that the binder built from a hashtable keeps a C# ID of 0,
	/// whatever its <c>Id</c> note property holds.
	/// </remarks>
	/// <param name="argument">The argument, or one element of it, which can be <see langword="null"/>.</param>
	/// <param name="nullBehavior">What happens when <paramref name="argument"/> gives no ID.</param>
	/// <param name="predicate">The range that the ID must be in.</param>
	/// <exception cref="ValidationMetadataException">Thrown when the ID is out of range, or when
	/// <paramref name="nullBehavior"/> refuses an argument that gives no ID or one that gives an ID.</exception>
	internal static void DoValidation(object? argument, InputNullBehavior nullBehavior, IdValidator predicate)
	{
		DoValidation(GetId(argument), nullBehavior, predicate);
	}
	/// <summary>
	/// Gets the ID that an argument, or one element of it, gives as a <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The pipeable interface that the ID is read through.</typeparam>
	/// <param name="element">The argument or element, which can be <see langword="null"/>.</param>
	/// <returns>
	/// The ID that <paramref name="element"/>'s <see cref="IPipeable{T}.GetId"/> returns, or <see langword="null"/> when
	/// <paramref name="element"/> is <see langword="null"/>.
	/// </returns>
	/// <exception cref="ValidationMetadataException">Thrown when <paramref name="element"/> isn't a
	/// <typeparamref name="T"/>.</exception>
	internal static int? GetIdFromPipeable<T>(object? element) where T : IPipeable<T>
	{
		return element switch
		{
			null => null,
			T pipeable => pipeable.GetId(),
			_ => ThrowNotPipeable<T>(),
		};
	}
	/// <summary>
	/// Gets the range check for the specified range.
	/// </summary>
	/// <param name="kind">The range that IDs are checked against.</param>
	/// <returns>The check that points at the method for <paramref name="kind"/>, or at the one for
	/// <see cref="ValidateRangeKind.Positive"/> when <paramref name="kind"/> is undefined.</returns>
	internal static IdValidator GetIdValidator(ValidateRangeKind kind)
	{
		return kind switch
		{
			ValidateRangeKind.NonNegative => new(&MustBeNonNegative),
			ValidateRangeKind.Negative => new(&MustBeNegative),
			ValidateRangeKind.NonPositive => new(&MustBeNonPositive),
			ValidateRangeKind.Positive or _ => new(&MustBePositive),
		};
	}

	/// <summary>
	/// Checks an ID against a range and fails validation when it's out of range.
	/// </summary>
	/// <param name="id">The ID to check.</param>
	/// <param name="validatorPtr">The range check.</param>
	/// <exception cref="ValidationMetadataException">Thrown when <paramref name="id"/> is out of the range, with a
	/// message that names the range.</exception>
	private static void ValidateId(int id, IdValidator validatorPtr)
	{
		if (!validatorPtr.Invoke(id, out ValidateRangeKind kind))
		{
			throw new ValidationMetadataException($"The argument's ID is not in the acceptable range of values. Expected value to be '{kind}'.");
		}
	}

	/// <summary>
	/// Determines whether an ID is greater than zero.
	/// </summary>
	/// <param name="id">The ID to check.</param>
	/// <param name="kind">When the method returns, <see cref="ValidateRangeKind.Positive"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="id"/> is greater than zero; otherwise,
	/// <see langword="false"/>.</returns>
	private static bool MustBePositive(int id, out ValidateRangeKind kind)
	{
		kind = ValidateRangeKind.Positive;
		return id > 0;
	}
	/// <summary>
	/// Determines whether an ID is less than zero.
	/// </summary>
	/// <param name="id">The ID to check.</param>
	/// <param name="kind">When the method returns, <see cref="ValidateRangeKind.Negative"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="id"/> is less than zero; otherwise,
	/// <see langword="false"/>.</returns>
	private static bool MustBeNegative(int id, out ValidateRangeKind kind)
	{
		kind = ValidateRangeKind.Negative;
		return id < 0;
	}
	/// <summary>
	/// Determines whether an ID is zero or greater.
	/// </summary>
	/// <param name="id">The ID to check.</param>
	/// <param name="kind">When the method returns, <see cref="ValidateRangeKind.NonNegative"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="id"/> is zero or greater; otherwise,
	/// <see langword="false"/>.</returns>
	private static bool MustBeNonNegative(int id, out ValidateRangeKind kind)
	{
		kind = ValidateRangeKind.NonNegative;
		return id >= 0;
	}
	/// <summary>
	/// Determines whether an ID is zero or less.
	/// </summary>
	/// <param name="id">The ID to check.</param>
	/// <param name="kind">When the method returns, <see cref="ValidateRangeKind.NonPositive"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="id"/> is zero or less; otherwise,
	/// <see langword="false"/>.</returns>
	private static bool MustBeNonPositive(int id, out ValidateRangeKind kind)
	{
		kind = ValidateRangeKind.NonPositive;
		return id <= 0;
	}

	/// <summary>
	/// Fails validation for an argument that gives no ID.
	/// </summary>
	/// <param name="argument">The missing ID, which the message doesn't show.</param>
	/// <exception cref="ValidationMetadataException">Always thrown.</exception>
	[DoesNotReturn]
	private static void ThrowIsNull(object? argument)
	{
		throw new ValidationMetadataException($"{nameof(argument)}'s ID value CANNOT be null.");
	}
	/// <summary>
	/// Fails validation for an argument that gives an ID when none is allowed.
	/// </summary>
	/// <param name="argument">The ID, which the message doesn't show.</param>
	/// <exception cref="ValidationMetadataException">Always thrown.</exception>
	[DoesNotReturn]
	private static void ThrowIsNotNull(object? argument)
	{
		throw new ValidationMetadataException($"{nameof(argument)}'s ID value MUST be null.");
	}
	/// <summary>
	/// Fails validation for an argument that isn't a <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The pipeable interface that the ID is read through.</typeparam>
	/// <returns>Never returns. The return type lets a <see langword="switch"/> expression arm call the method.</returns>
	/// <exception cref="ValidationMetadataException">Always thrown, with a message that names
	/// <typeparamref name="T"/>.</exception>
	[DoesNotReturn]
	private static int? ThrowNotPipeable<T>()
	{
		throw new ValidationMetadataException($"Unable to validate argument as '{typeof(T).GetName()}'.");
	}

	/// <summary>
	/// Initializes the class's cached delegates: the one that gives no ID, the cache of the delegates that read an
	/// <see cref="Either{T1, T2}"/>'s ID, and the generic definition that they're made from.
	/// </summary>
	/// <exception cref="MethodException">Thrown when the generic definition of
	/// <see cref="GetIdFromIdOrObject{T}(object)"/> can't be found.</exception>
	// Delegate caching
	static IdValidationHelper()
	{
		s_fallbackNull = _ => null;
		s_idOrObjectMethod = typeof(IdValidationHelper).GetMethod(nameof(GetIdFromIdOrObject), BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new MethodException($"Unable to find the '{nameof(GetIdFromIdOrObject)}' method.");
		s_fromIdOrObject = new();
	}

	/// <summary>
	/// Gets the ID that an argument, or one element of it, gives.
	/// </summary>
	/// <remarks>
	/// A model is both an <see cref="IHasId"/> and a <see cref="PSObject"/>, and its C# <see cref="IHasId.Id"/> is the ID
	/// that a cmdlet acts on, so <see cref="IHasId"/> is checked before <see cref="PSObject"/>. A string gives no ID
	/// and is checked first, since it's the commonest argument that gives none.
	/// </remarks>
	/// <param name="argument">The argument, or one element of it, which can be <see langword="null"/>.</param>
	/// <returns>
	/// The <see cref="IHasId.Id"/> of an <see cref="IHasId"/>, the <see cref="int"/> that an <see cref="Either{T1, T2}"/>
	/// of <see cref="int"/> and <see cref="string"/> holds, the <c>Id</c> property of a <see cref="PSObject"/>, an
	/// <see cref="int"/> itself, or the <see cref="int"/> that an <see cref="Either{T1, T2}"/> of <see cref="int"/> and a
	/// reference type holds or else the ID that its object gives; otherwise, <see langword="null"/>.
	/// </returns>
	private static int? GetId(object? argument)
	{
		return argument switch
		{
			null or string => null,
			IHasId hasId => hasId.Id,
			Either<string, int> or Either<int, string> => GetIdFromEither(argument),
			PSObject pso => GetIdFromPSObject(pso),
			int id => id,
			_ => s_fromIdOrObject.GetOrAdd(argument.GetType(), CreateIdOrObjectDelegate)(argument),
		};
	}

	/// <summary>
	/// Creates the delegate that reads the ID from a boxed <see cref="Either{T1, T2}"/> of <see cref="int"/> and a
	/// reference type.
	/// </summary>
	/// <param name="argumentType">The runtime type of an argument that <see cref="GetId(object?)"/> doesn't otherwise
	/// read.</param>
	/// <returns>
	/// A delegate that calls <see cref="GetIdFromIdOrObject{T}(object)"/> when <paramref name="argumentType"/> is such a
	/// union; otherwise, one that always returns <see langword="null"/>.
	/// </returns>
	private static GetIdDelegate CreateIdOrObjectDelegate(Type argumentType)
	{
		if (!argumentType.IsGenericType || argumentType.GetGenericTypeDefinition() != typeof(Either<,>))
		{
			return s_fallbackNull;
		}

		Type[] arguments = argumentType.GenericTypeArguments;
		if (arguments[0] != typeof(int) || arguments[1].IsValueType)
		{
			return s_fallbackNull;
		}

		return s_idOrObjectMethod.MakeGenericMethod(arguments[1]).CreateDelegate<GetIdDelegate>();
	}

	/// <summary>
	/// Gets the ID that an <see cref="Either{T1, T2}"/> of <see cref="int"/> and <typeparamref name="T"/> holds.
	/// </summary>
	/// <typeparam name="T">The type of the object that the union holds in place of an ID.</typeparam>
	/// <param name="either">The boxed union.</param>
	/// <returns>
	/// The <see cref="int"/> that the union holds, the ID that its <typeparamref name="T"/> gives, as
	/// <see cref="GetId(object?)"/> reads it, or <see langword="null"/> when the union holds neither.
	/// </returns>
	private static int? GetIdFromIdOrObject<T>(object either) where T : class
	{
		Either<int, T> idOrObject = (Either<int, T>)either;
		return idOrObject.Index switch
		{
			1 => idOrObject.AsT1,
			2 => GetId(idOrObject.AsT2),
			_ => null,
		};
	}

	/// <summary>
	/// Gets the <see cref="int"/> that an <see cref="Either{T1, T2}"/> of <see cref="int"/> and <see cref="string"/> holds.
	/// </summary>
	/// <param name="either">The boxed union.</param>
	/// <returns>The <see cref="int"/>, or <see langword="null"/> when the union holds its <see cref="string"/> or
	/// <paramref name="either"/> is another type.</returns>
	private static int? GetIdFromEither(object either)
	{
		return either switch
		{
			Either<int, string> asFirst when asFirst.IsT1 => asFirst.AsT1,
			Either<string, int> asSecond when asSecond.IsT2 => asSecond.AsT2,
			_ => null,
		};
	}

	/// <summary>
	/// Gets the value of an object's <c>Id</c> property.
	/// </summary>
	/// <param name="pso">The object.</param>
	/// <returns>The ID, or <see langword="null"/> when <paramref name="pso"/> has no <c>Id</c> property or its value isn't
	/// an <see cref="int"/>.</returns>
	private static int? GetIdFromPSObject(PSObject pso)
	{
		PSPropertyInfo? ppi = pso.Properties[Constants.Id];
		return ppi?.Value as int?;
	}

	private static readonly GetIdDelegate s_fallbackNull;
	/// <summary>
	/// The delegates that <see cref="GetId(object?)"/> reads an ID with, keyed by the runtime type of the argument, for
	/// the arguments that it doesn't otherwise read.
	/// </summary>
	private static readonly ConcurrentDictionary<Type, GetIdDelegate> s_fromIdOrObject;
	/// <summary>
	/// The generic definition of <see cref="GetIdFromIdOrObject{T}(object)"/>.
	/// </summary>
	private static readonly MethodInfo s_idOrObjectMethod;
}

