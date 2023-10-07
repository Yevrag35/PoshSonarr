using MG.Sonarr.Next.Services.Attributes;
using MG.Sonarr.Next.Services.Reflection;
using OneOf;
using System.Reflection;

namespace MG.Sonarr.Next.Services.Extensions
{
    public static class ExpressionExtensions
    {
        public static bool TryGetAsMember([ValidatedNotNull] this LambdaExpression expression, [NotNullWhen(true)] out MemberExpression? memberExpression)
        {
            if (expression.Body is MemberExpression memEx)
            {
                memberExpression = memEx;
                return true;
            }
            else if (expression.Body is UnaryExpression unEx && unEx.Operand is MemberExpression unExMem)
            {
                memberExpression = unExMem;
                return true;
            }

            memberExpression = null;
            return false;
        }

        public static bool TryGetAsSetter([ValidatedNotNull] this LambdaExpression expression, [NotNullWhen(true)] out FieldOrPropertyInfoSetter setter)
        {
            setter = default;
            OneOf<FieldInfo, PropertyInfo, object?> tempOne;
            if (expression.Body is MemberExpression memEx)
            {
                tempOne = GetAsEitherInfo(memEx);
            }
            else if (expression.Body is UnaryExpression unEx && unEx.Operand is MemberExpression unExMem)
            {
                tempOne = GetAsEitherInfo(unExMem);
            }
            else
            {
                tempOne = OneOf<FieldInfo, PropertyInfo, object?>.FromT2(null);
            }

            if (!tempOne.IsT2)
            {
                setter = tempOne.IsT0
                    ? new FieldOrPropertyInfoSetter(tempOne.AsT0)
                    : new FieldOrPropertyInfoSetter(tempOne.AsT1);
            }

            return !setter.IsEmpty;
        }

        /// <exception cref="ArgumentException"></exception>
        private static OneOf<FieldInfo, PropertyInfo, object?> GetAsEitherInfo(MemberExpression memberExpression)
        {
            switch (memberExpression.Member.MemberType)
            {
                case MemberTypes.Field:
                    return (FieldInfo)memberExpression.Member;

                case MemberTypes.Property:
                    return (PropertyInfo)memberExpression.Member;

                case MemberTypes.Constructor:
                case MemberTypes.Event:
                case MemberTypes.Method:
                case MemberTypes.TypeInfo:
                case MemberTypes.Custom:
                case MemberTypes.NestedType:
                case MemberTypes.All:
                default:
                    return (object?)null;
            }
        }
    }
}