using System.Collections;

namespace MG.Sonarr.Next.Shell.Attributes
{
    public sealed class ValidateTypeAttribute : ValidateArgumentsAttribute
    {
        readonly HashSet<Type> _mustBeOneOf;

        public ValidateTypeAttribute(params Type[] types)
        {
            ArgumentNullException.ThrowIfNull(types);
            _mustBeOneOf = new HashSet<Type>(types);
        }

        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (arguments is IEnumerable enumerable)
            {
                foreach (object? item in enumerable)
                {
                    GuardIsType(_mustBeOneOf, item);
                }
            }
            else
            {
                GuardIsType(_mustBeOneOf, arguments);
            }
        }

        private static void GuardIsType(IReadOnlySet<Type> types, object? item)
        {
            if (item is null || !types.Contains(item.GetType()))
            {
                throw new ValidationMetadataException("Argument did not match any of the types.");
            }
        }
    }
}

