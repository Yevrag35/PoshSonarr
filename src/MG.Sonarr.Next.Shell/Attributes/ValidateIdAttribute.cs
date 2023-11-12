using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using System.Reflection;
using System.Xml.Linq;

namespace MG.Sonarr.Next.Shell.Attributes
{
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
        readonly IdPredicate _predicate;

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
        public InputNullBehavior NullBehavior { get; }

        public ValidateIdAttribute(ValidateRangeKind kind)
            : this(kind, InputNullBehavior.EnforceNotNull)
        {
        }
        public ValidateIdAttribute(ValidateRangeKind kind, InputNullBehavior nullBehavior)
        {
            this.Kind = kind;
            this.NullBehavior = nullBehavior;
            _parameterType = typeof(object);
            _isValidatableType = false;
            _predicate = IdValidationHelper.GetValidation(in kind);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateIdAttribute"/> class. 
        /// This constructor uses a predefined <see cref="ValidateRangeKind"/> and uses the default <see langword="null"/>
        /// behavior, <see cref="InputNullBehavior.EnforceNotNull"/>.
        /// </summary>
        /// <param name="kind">The predefined range to validate an ID against.</param>
        public ValidateIdAttribute(ValidateRangeKind kind, Type parameterType)
            : this(kind, parameterType, InputNullBehavior.EnforceNotNull)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateIdAttribute"/> class. 
        /// This constructor uses a predefined <see cref="ValidateRangeKind"/> and the specfied
        /// <see cref="InputNullBehavior"/>.
        /// </summary>
        /// <param name="kind">The predefined range to validate an ID against.</param>
        /// <param name="nullBehavior">
        ///     The validation behavior when a passed <see cref="IHasId"/> or <see cref="PSObject"/> instance is 
        ///     <see langword="null"/> or when a <see cref="PSObject"/> instance does not contain an "Id" property.
        /// </param>
        public ValidateIdAttribute(ValidateRangeKind kind, Type parameterType, InputNullBehavior nullBehavior)
        {
            ArgumentNullException.ThrowIfNull(parameterType);
            this.Kind = kind;
            this.NullBehavior = nullBehavior;
            _parameterType = parameterType;
            _isValidatableType = IdValidationHelper.TryGetMethodInfo(parameterType);
            _predicate = IdValidationHelper.GetValidation(in kind);
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
                    $"Unable to validate argument as '{_parameterType.GetTypeName()}'.", e);
            }

            IdValidationHelper.DoValidation(possibleId: possible, this.NullBehavior, _predicate);
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
        readonly IdPredicate _predicate;
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
        public InputNullBehavior NullBehavior { get; }

        public ValidateIdsAttribute(ValidateRangeKind kind)
            : this(kind, InputNullBehavior.EnforceNotNull)
        {
        }
        public ValidateIdsAttribute(ValidateRangeKind kind, InputNullBehavior nullBehavior)
        {
            this.Kind = kind;
            this.NullBehavior = nullBehavior;
            _parameterType = typeof(object);
            _isValidatableType = false;
            _predicate = IdValidationHelper.GetValidation(in kind);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateIdsAttribute"/> class. 
        /// This constructor uses a predefined <see cref="ValidateRangeKind"/> and uses the default <see langword="null"/>
        /// behavior, <see cref="InputNullBehavior.EnforceNotNull"/>.
        /// </summary>
        /// <param name="kind">The predefined range to validate the element's ID against.</param>
        public ValidateIdsAttribute(ValidateRangeKind kind, Type parameterType)
            : this(kind, parameterType, InputNullBehavior.EnforceNotNull)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateIdsAttribute"/> class. 
        /// This constructor uses a predefined <see cref="ValidateRangeKind"/> and the specfied
        /// <see cref="InputNullBehavior"/>.
        /// </summary>
        /// <param name="kind">The predefined range to validate the element's ID against.</param>
        /// <param name="nullBehavior">
        ///     The validation behavior when an element is <see cref="IHasId"/> or <see cref="PSObject"/> and
        ///     <see langword="null"/> OR when a <see cref="PSObject"/> element does not contain an "Id" property.
        /// </param>
        public ValidateIdsAttribute(ValidateRangeKind kind, Type parameterType, InputNullBehavior nullBehavior)
        {
            ArgumentNullException.ThrowIfNull(parameterType);
            this.Kind = kind;
            this.NullBehavior = nullBehavior;
            _parameterType = parameterType;
            _isValidatableType = IdValidationHelper.TryGetMethodInfo(parameterType);
            _predicate = IdValidationHelper.GetValidation(in kind);
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
                    $"Unable to validate argument as '{_parameterType.GetTypeName()}'.", e);
            }

            IdValidationHelper.DoValidation(possibleId: possible, this.NullBehavior, _predicate);
        }
    }

    /// <summary>
    /// A validation delegate used for validating "Id" properties of type <see cref="int"/>.
    /// </summary>
    /// <param name="id">The ID value to validate.</param>
    /// <param name="kind">
    ///     When the delegate returns, this parameter will be populated with the <see cref="ValidateRangeKind"/>
    ///     value that best matches the type of validation used. Normally, used in the construction for a failed 
    ///     validation <see cref="Exception.Message"/>.
    /// </param>
    /// <returns>
    ///     <see langword="true"/>, if <paramref name="id"/> passes validation; otherwise, <see langword="false"/>.
    /// </returns>
    delegate bool IdPredicate(in int id, out ValidateRangeKind kind);
    file static class IdValidationHelper
    {
        static readonly Dictionary<Type, MethodInfo> _getIds = new(5);
        static readonly MethodInfo _getIdMethod = typeof(IdValidationHelper).GetMethod(nameof(GetId), BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MethodException("Unable to find 'GetId' method...??!?");
        private static int? GetId<T>(T pipeable) where T : IValidatableId<T>, IPipeable<T>
        {
            return T.GetValidatableId(pipeable);
        }

        internal static void DoValidation(int? possibleId, InputNullBehavior nullBehavior, IdPredicate predicate)
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

        /// <exception cref="ValidationMetadataException"/>
        internal static void DoValidation(object? argument, InputNullBehavior nullBehavior, IdPredicate predicate)
        {
            int? possibleId = argument switch
            {
                IHasId idObj => idObj.Id,
                PSObject pso => GetIdFromPSObject(pso),
                int id => id,
                _ => null,
            };

            DoValidation(possibleId, nullBehavior, predicate);
        }
        internal static int? ExecuteMethod(Type parameterType, object element)
        {
            ArgumentNullException.ThrowIfNull(parameterType);
            ArgumentNullException.ThrowIfNull(element);

            return (int?)_getIds[parameterType].Invoke(null, new object[] { element });
        }
        internal static IdPredicate GetValidation(in ValidateRangeKind kind)
        {
            switch (kind)
            {
                case ValidateRangeKind.Positive:
                    return MustBePositive;

                case ValidateRangeKind.NonNegative:
                    return MustBeNonNegative;

                case ValidateRangeKind.Negative:
                    return MustBeNegative;

                case ValidateRangeKind.NonPositive:
                    return MustBeNonPositive;

                default:
                    goto case ValidateRangeKind.Positive;
            }
        }

        /// <exception cref="ValidationMetadataException"></exception>
        private static void ValidateId(int id, IdPredicate validationFunc)
        {
            if (!validationFunc(in id, out ValidateRangeKind kind))
            {
                throw new ValidationMetadataException($"The argument's ID is not in the acceptable range of values. Expected value to be '{kind}'.");
            }
        }

        private static bool MustBePositive(in int id, out ValidateRangeKind kind)
        {
            kind = ValidateRangeKind.Positive;
            return id > 0;
        }
        private static bool MustBeNegative(in int id, out ValidateRangeKind kind)
        {
            kind = ValidateRangeKind.Negative;
            return id < 0;
        }
        private static bool MustBeNonNegative(in int id, out ValidateRangeKind kind)
        {
            kind = ValidateRangeKind.NonNegative;
            return id >= 0;
        }
        private static bool MustBeNonPositive(in int id, out ValidateRangeKind kind)
        {
            kind = ValidateRangeKind.NonPositive;
            return id <= 0;
        }

        private static int? GetIdFromPSObject(PSObject pso)
        {
            PSPropertyInfo? ppi = pso?.Properties[Constants.ID];
            return ppi?.Value as int?;
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

        internal static bool TryGetMethodInfo(Type parameterType)
        {
            if (_getIds.ContainsKey(parameterType))
            {
                return true;
            }
            else if (!TryGetMatchingPipeableInterface(parameterType))
            {
                return false;
            }

            MethodInfo genMeth = _getIdMethod.MakeGenericMethod(parameterType);
            return _getIds.TryAdd(parameterType, genMeth);
        }
        private static bool TryGetMatchingPipeableInterface(Type parameterType)
        {
            Type[] interfaces = parameterType.GetInterfaces();
            if (interfaces.Length <= 0)
            {
                return false;
            }

            DoubleBool dub = DoubleBool.InitializeNew();

            foreach (Type @interface in interfaces.OrderByDescending(x => x.Name))
            {
                if (InterfaceGenericsEqual(@interface, parameterType))
                {
                    if (@interface.Name.StartsWith("IValidatableId", StringComparison.Ordinal))
                    {
                        dub.Bool1 = true;
                    }
                    else if (@interface.Name.StartsWith("IPipeable", StringComparison.Ordinal))
                    {
                        dub.Bool2 = true;
                    }
                }

                if (dub)
                {
                    break;
                }
            }

            return dub;
        }

        private static bool InterfaceGenericsEqual(Type interfaceType, Type parameterType)
        {
            return interfaceType.GenericTypeArguments.Length > 0
                   &&
                   parameterType.Equals(interfaceType.GenericTypeArguments[0]);
        }

        private ref struct DoubleBool
        {
            public bool Bool1;
            public bool Bool2;

            private DoubleBool(bool initialize)
            {
                Bool1 = initialize;
                Bool2 = initialize;
            }

            internal static DoubleBool InitializeNew() => new(false);

            public static implicit operator bool(DoubleBool dub) => dub.Bool1 && dub.Bool2;
        }
    }
}

