using MG.Sonarr.Next.Models;
using System.Collections;

namespace MG.Sonarr.Next.Shell.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ValidateIdAttribute : ValidateArgumentsAttribute
    {
        public ValidateRangeKind Kind { get; set; }

        public ValidateIdAttribute(ValidateRangeKind kind)
        {
            this.Kind = kind;
        }

        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            Func<IHasId, bool> predicate = GetValidation(this.Kind);

            if (arguments is IHasId hasId)
            {
                this.ValidateId(hasId, predicate);
            }
            else if (arguments is IEnumerable collection)
            {
                foreach (IHasId item in collection.OfType<IHasId>())
                {
                    this.ValidateId(item, predicate);
                }
            }
        }

        private void ValidateId(IHasId hasId, Func<IHasId, bool> validationFunc)
        {
            if (!validationFunc(hasId))
            {
                throw new ValidationMetadataException($"The object's ID is not in the acceptable range of values. Expected '{this.Kind}'.");
            }
        }

        private static Func<IHasId, bool> GetValidation(ValidateRangeKind kind)
        {
            return kind switch
            {
                ValidateRangeKind.Positive => o => o.Id > 0,
                ValidateRangeKind.NonNegative => o => o.Id >= 0,
                ValidateRangeKind.Negative => o => o.Id < 0,
                ValidateRangeKind.NonPositive => o => o.Id <= 0,
                _ => o => o.Id > 0,
            };
        }
    }
}

