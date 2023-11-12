using MG.Sonarr.Next.Enums;
using MG.Sonarr.Next.Reflection;
using System.Reflection;

namespace MG.Sonarr.Next.Extensions
{
    /// <summary>
    /// Custom extension methods for <see cref="Expression"/> and derived classes.
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Attempts to retrieve the body of a <see cref="LambdaExpression"/> as a member 
        /// declaration.
        /// </summary>
        /// <param name="expression">
        ///     The expression whose body could possibly be a member expression.
        /// </param>
        /// <param name="memberExpression">
        ///     When this method returns <see langword="true"/>, the 
        ///     <see cref="MemberExpression"/> read from the body of 
        ///     <paramref name="expression"/>; otherwise, a <see langword="null"/> reference.
        ///     <para>
        ///         This parameter is passed uninitialized.
        ///     </para>
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if <see cref="LambdaExpression.Body"/> is 
        ///     <see cref="MemberExpression"/> or is a <see cref="UnaryExpression"/> whose 
        ///     <see cref="UnaryExpression.Operand"/> is <see cref="MemberExpression"/>; otherwise,
        ///     <see langword="false"/>.
        /// </returns>
        public static bool TryGetAsMember(this LambdaExpression expression, [NotNullWhen(true)] out MemberExpression? memberExpression)
        {
            ArgumentNullException.ThrowIfNull(expression);

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

        public static FieldOrPropertyInfo GetMemberInfo(this LambdaExpression expression)
        {
            MemberExpression? memEx = expression.Body as MemberExpression;
            if (memEx is null && expression.Body is UnaryExpression unEx && unEx.Operand is MemberExpression unMemEx)
            {
                memEx = unMemEx;
            }

            if (memEx is null)
            {
                return default;
            }

            MemberInfo member = memEx.Member; MemberTypes enumValue = member.MemberType;

            foreach (MemberTypes flag in enumValue.Enumerate())
            {
                switch (flag)
                {
                    case MemberTypes.Property:
                        return (PropertyInfo)member;

                    case MemberTypes.Field:
                        return (FieldInfo)member;

                    default:
                        break;
                }
            }

            return default;
        }

        /// <summary>
        /// Attempts to retrieve a <see cref="LambdaExpression"/> instance's body as a
        /// <see cref="MemberExpression"/> of a defined <see cref="PropertyInfo"/> or 
        /// <see cref="FieldInfo"/> that is settable.
        /// </summary>
        /// <param name="expression">The expression to check.</param>
        /// <param name="setter">
        ///     When this method returns, the <see cref="FieldOrPropertyInfo"/>
        ///     of the expression's <see cref="MemberExpression.Member"/>; otherwise, the
        ///     default instance of <see cref="FieldOrPropertyInfo"/>.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if <paramref name="expression"/>'s body is a valid 
        ///     <see cref="MemberExpression"/> and that <see cref="MemberExpression.Member"/> is a
        ///     settable <see cref="PropertyInfo"/> or <see cref="FieldInfo"/>; otherwise,
        ///     <see langword="false"/>.
        /// </returns>
        public static bool TryGetAsSetter(this LambdaExpression expression, [NotNullWhen(true)] out IMemberSetter? setter)
        {
            MemberExpression? memEx = expression.Body as MemberExpression;
            if (memEx is null && expression.Body is UnaryExpression unEx && unEx.Operand is MemberExpression unMemEx)
            {
                memEx = unMemEx;
            }

            if (memEx is null)
            {
                setter = default;
                return false;
            }

            MemberTypes enumValue = memEx.Member.MemberType;
            FlagEnumerator<MemberTypes> enumerator = new(enumValue);

            while (enumerator.MoveNext())
            {
                switch (enumerator.Current)
                {
                    case MemberTypes.Property:
                        setter = new FieldOrPropertyInfo((PropertyInfo)memEx.Member);
                        return setter.CanSet;

                    case MemberTypes.Field:
                        setter = new FieldOrPropertyInfo((FieldInfo)memEx.Member);
                        return setter.CanSet;

                    default:
                        break;
                }
            }

            setter = default;
            return false;
        }

        //private static Either<FieldInfo, PropertyInfo, object?> GetAsEitherInfo(MemberExpression memberExpression)
        //{
        //    switch (memberExpression.Member.MemberType)
        //    {
        //        case MemberTypes.Field:
        //            return (FieldInfo)memberExpression.Member;

        //        case MemberTypes.Property:
        //            return (PropertyInfo)memberExpression.Member;

        //        case MemberTypes.Constructor:
        //        case MemberTypes.Event:
        //        case MemberTypes.Method:
        //        case MemberTypes.TypeInfo:
        //        case MemberTypes.Custom:
        //        case MemberTypes.NestedType:
        //        case MemberTypes.All:
        //        default:
        //            return (object?)null;
        //    }
        //}
    }
}