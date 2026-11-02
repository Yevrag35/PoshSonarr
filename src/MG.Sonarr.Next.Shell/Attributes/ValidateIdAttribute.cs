using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Unions;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Shell.Attributes;

/// <summary>
/// A validation attribute decorated on a parameter that is validated to either a <see cref="IHasId"/> implementing 
/// class or a <see cref="PSObject"/> instance with an <see cref="int"/> "Id" property as well as
/// making sure it adheres to the specified <see cref="ValidateRangeKind"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ValidateIdAttribute : ValidateArgumentsAttribute
{
	readonly bool _isValidatableType;
	readonly Type _parameterType;
	readonly IdValidator _predicate;

	/// <summary>
	/// The range the "Id" value must be in to pass validation.
	/// </summary>
	public ValidateRangeKind Kind { get; }
	/// <summary>
	/// The validation behavior when a passed <see cref="IHasId"/> or <see cref="PSObject"/> instance is 
	/// <see langword="null"/> or when a <see cref="PSObject"/> instance does not contain an "Id" property.
	/// </summary>
	/// <remarks>
	///     When not specified in the constructor, the default value is <see cref="InputNullBehavior.EnforceNotNull"/> 
	///     throwing a <see cref="ValidationMetadataException"/> on encountering <see langword="null"/> values.
	/// </remarks>
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
		_parameterType = typeof(object);
		_isValidatableType = false;
		_predicate = IdValidationHelper.GetIdValidator(kind);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ValidateIdAttribute"/> class. 
	/// This constructor uses a predefined <see cref="ValidateRangeKind"/> and uses the default <see langword="null"/>
	/// behavior, <see cref="InputNullBehavior.EnforceNotNull"/>.
	/// </summary>
	/// <param name="kind">The predefined range to validate an ID against.</param>
	public ValidateIdAttribute(ValidateRangeKind kind, Type parameterType)
	{
		ArgumentNullException.ThrowIfNull(parameterType);
		this.Kind = kind;
		_parameterType = parameterType;
		_isValidatableType = IdValidationHelper.TryGetMethodInfo(parameterType);
		_predicate = IdValidationHelper.GetIdValidator(kind);
	}

	protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
	{
		if (!_isValidatableType || arguments is null)
		{
			IdValidationHelper.DoValidation(argument: arguments, this.NullBehavior, _predicate);
			return;
		}

		int? possible;
		try
		{
			possible = IdValidationHelper.ExecuteMethod(_parameterType, arguments);
		}
		catch (Exception e)
		{
			throw new ValidationMetadataException(
				$"Unable to validate argument as '{_parameterType.GetName()}'.", e);
		}

		IdValidationHelper.DoValidation(possible, this.NullBehavior, _predicate);
	}
}
/// <summary>
/// A validation attribute decorated on a parameter that accepts an <see cref="Array"/> or collection of elements.  
/// Each enumerated element is validated to either a <see cref="IHasId"/> implementing class or a <see cref="PSObject"/>
/// instance with an "Id" property. Each element's ID property is validated to make sure it adheres to the specified
/// <see cref="ValidateRangeKind"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ValidateIdsAttribute : ValidateEnumeratedArgumentsAttribute
{
	readonly bool _isValidatableType;
	readonly Type _parameterType;
	private readonly IdValidator _predicate;
	/// <summary>
	/// The range the "Id" value must be in to pass validation.
	/// </summary>
	public ValidateRangeKind Kind { get; }
	/// <summary>
	/// The validation behavior when a passed <see cref="IHasId"/> or <see cref="PSObject"/> instance is 
	/// <see langword="null"/> or when a <see cref="PSObject"/> instance does not contain an "Id" property.
	/// </summary>
	/// <remarks>
	///     When not specified in the constructor, the default value is <see cref="InputNullBehavior.EnforceNotNull"/> 
	///     throwing a <see cref="ValidationMetadataException"/> on encountering <see langword="null"/> values.
	/// </remarks>
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
		_parameterType = typeof(object);
		_isValidatableType = false;
		_predicate = IdValidationHelper.GetIdValidator(kind);
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="ValidateIdsAttribute"/> class. 
	/// This constructor uses a predefined <see cref="ValidateRangeKind"/> and the specfied
	/// <see cref="InputNullBehavior"/>.
	/// </summary>
	/// <param name="kind">The predefined range to validate the element's ID against.</param>
	public ValidateIdsAttribute(ValidateRangeKind kind, Type parameterType)
	{
		ArgumentNullException.ThrowIfNull(parameterType);
		this.Kind = kind;
		_parameterType = parameterType;
		_isValidatableType = IdValidationHelper.TryGetMethodInfo(parameterType);
		_predicate = IdValidationHelper.GetIdValidator(kind);
	}

	protected override void ValidateElement(object element)
	{
		if (!_isValidatableType || element is null)
		{
			IdValidationHelper.DoValidation(argument: element, this.NullBehavior, _predicate);
			return;
		}

		int? possible;
		try
		{
			possible = IdValidationHelper.ExecuteMethod(_parameterType, element);
		}
		catch (Exception e)
		{
			throw new ValidationMetadataException(
				$"Unable to validate argument as '{_parameterType.GetName()}'.", e);
		}

		IdValidationHelper.DoValidation(possibleId: possible, this.NullBehavior, _predicate);
	}
}

/// <summary>
/// Provides a delegate-based mechanism for validating integer identifiers and determining their range classification.
/// </summary>
/// <remarks>This struct encapsulates a function pointer used to validate IDs and classify them according to a
/// specified range kind. It is intended for internal use where performance and direct delegate invocation are required.
/// The struct is immutable and thread-safe.</remarks>
[StructLayout(LayoutKind.Sequential, Size = 8)]
internal readonly unsafe struct IdValidator
{
	private readonly delegate* managed<int, out ValidateRangeKind, bool> _ptr;

	internal IdValidator(delegate* managed<int, out ValidateRangeKind, bool> ptr)
	{
		ArgumentNullException.ThrowIfNull(ptr);
		_ptr = ptr;
	}

	internal bool Invoke(int id, out ValidateRangeKind kind)
	{
		return _ptr(id, out kind);
	}
}

/// <summary>
/// Provides helper methods for validating and retrieving identifiers from objects that implement pipeable interfaces.
/// </summary>
/// <remarks>This class is intended for internal use in scenarios where object identifiers must be validated or
/// extracted according to specific rules. It supports validation of identifier values against defined range constraints
/// and facilitates retrieval of identifiers from types implementing the <see cref="IPipeable{T}"/> interface. All members are static
/// and thread-safe.</remarks>
file static class IdValidationHelper
{
	/// <summary>
	/// Represents a method that retrieves an identifier for the specified element.
	/// </summary>
	/// <param name="element">The element for which to obtain an identifier. Cannot be null.</param>
	/// <returns>An integer representing the identifier of the element, or null if no identifier is available.</returns>
	delegate int? GetIdDelegate(object element);

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
	internal static int? ExecuteMethod(Type parameterType, object element)
	{
		ArgumentNullException.ThrowIfNull(element);

		return s_getIds[parameterType](element);
	}
	internal static unsafe IdValidator GetIdValidator(ValidateRangeKind kind)
	{
		return kind switch
		{
			ValidateRangeKind.NonNegative => new(&MustBeNonNegative),
			ValidateRangeKind.Negative => new(&MustBeNegative),
			ValidateRangeKind.NonPositive => new(&MustBeNonPositive),
			ValidateRangeKind.Positive or _ => new(&MustBePositive),
		};
	}

	private static void ValidateId(int id, IdValidator validatorPtr)
	{
		if (!validatorPtr.Invoke(id, out ValidateRangeKind kind))
		{
			throw new ValidationMetadataException($"The argument's ID is not in the acceptable range of values. Expected value to be '{kind}'.");
		}
	}

	private static bool MustBePositive(int id, out ValidateRangeKind kind)
	{
		kind = ValidateRangeKind.Positive;
		return id > 0;
	}
	private static bool MustBeNegative(int id, out ValidateRangeKind kind)
	{
		kind = ValidateRangeKind.Negative;
		return id < 0;
	}
	private static bool MustBeNonNegative(int id, out ValidateRangeKind kind)
	{
		kind = ValidateRangeKind.NonNegative;
		return id >= 0;
	}
	private static bool MustBeNonPositive(int id, out ValidateRangeKind kind)
	{
		kind = ValidateRangeKind.NonPositive;
		return id <= 0;
	}

	/// <exception cref="ValidationMetadataException"></exception>
	[DoesNotReturn]
	private static void ThrowIsNull(object? argument)
	{
		throw new ValidationMetadataException($"{nameof(argument)}'s ID value CANNOT be null.");
	}
	/// <exception cref="ValidationMetadataException"></exception>
	[DoesNotReturn]
	private static void ThrowIsNotNull(object? argument)
	{
		throw new ValidationMetadataException($"{nameof(argument)}'s ID value MUST be null.");
	}

	// Delegate caching
	static IdValidationHelper()
	{
		GetIdDelegate fromEither = GetIdFromEither;

		s_fallbackNull = _ => null;
		s_fromPso = pso => GetIdFromPSObject((PSObject)pso);
		s_getIds = new(
		[
			new(typeof(PSObject), s_fromPso),
			new(typeof(Either<int, string>), fromEither),
			new(typeof(Either<string, int>), fromEither),
		]);
		s_method = typeof(IdValidationHelper).GetMethod(nameof(GetIdFromPipeable), BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new MethodException("Unable to find 'GetIdFromObject' method...??!?");
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
	private static readonly GetIdDelegate s_fromPso;
	private static readonly ConcurrentDictionary<Type, GetIdDelegate> s_getIds;
	/// <summary>
	/// The generic definition of <see cref="GetIdFromIdOrObject{T}(object)"/>.
	/// </summary>
	private static readonly MethodInfo s_idOrObjectMethod;
	private static readonly MethodInfo s_method;
	internal static bool TryGetMethodInfo(Type parameterType)
	{
		return s_getIds.GetOrAdd(parameterType, CreateDelegate) != s_fallbackNull;
	}

	private static GetIdDelegate CreateDelegate(Type parameterType)
	{
		if (!TryGetMatchingPipeableInterface(parameterType))
		{
			return typeof(PSObject).IsAssignableFrom(parameterType) ? s_fromPso : s_fallbackNull;
		}

		MethodInfo genMeth = s_method.MakeGenericMethod(parameterType);
		return genMeth.CreateDelegate<GetIdDelegate>();
	}
	
	private static int? GetIdFromPipeable<T>(object element) where T : IPipeable<T>
	{
		return ((T)element).GetId();
	}
	private static bool TryGetMatchingPipeableInterface(Type parameterType)
	{
		Type[] interfaces = parameterType.GetInterfaces();
		if (interfaces.Length == 0)
		{
			return false;
		}

		foreach (Type @interface in interfaces.OrderByDescending(x => x.Name))
		{
			if (InterfaceGenericsEqual(@interface, parameterType))
			{
				if (@interface.Name.StartsWith(nameof(IPipeable<>), StringComparison.Ordinal))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static bool InterfaceGenericsEqual(Type interfaceType, Type parameterType)
	{
		return interfaceType.GenericTypeArguments.Length > 0
			   &&
			   parameterType.Equals(interfaceType.GenericTypeArguments[0]);
	}
}

