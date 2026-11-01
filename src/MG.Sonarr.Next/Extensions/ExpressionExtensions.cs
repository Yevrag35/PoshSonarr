using MG.Sonarr.Next.Enums;
using MG.Sonarr.Next.Reflection;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods that read the field or property that a lambda expression accesses.
/// </summary>
/// <remarks>
/// Each method accepts a body that's a member access, such as <c>x =&gt; x.Name</c>, or a conversion of one, such
/// as the <c>(object)x.Id</c> that the compiler emits for a value-type member in a lambda that returns
/// <see cref="object"/>.
/// </remarks>
public static class ExpressionExtensions
{
	/// <summary>
	/// Tries to get the member access that a lambda expression's body consists of.
	/// </summary>
	/// <param name="expression">The lambda expression to read.</param>
	/// <param name="memberExpression">
	/// When this method returns <see langword="true"/>, the <see cref="MemberExpression"/> that the body is or
	/// converts; otherwise, <see langword="null"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <see cref="LambdaExpression.Body"/> is a <see cref="MemberExpression"/>, or is a
	/// <see cref="UnaryExpression"/> whose <see cref="UnaryExpression.Operand"/> is one; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
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

	/// <summary>
	/// Gets the field or property that a lambda expression's body accesses.
	/// </summary>
	/// <param name="expression">The lambda expression to read.</param>
	/// <returns>
	/// A <see cref="FieldOrPropertyInfo"/> for the field or property that the body accesses, or an empty one, whose
	/// <see cref="FieldOrPropertyInfo.IsEmpty"/> is <see langword="true"/>, when the body isn't a field or property
	/// access.
	/// </returns>
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

		MemberInfo member = memEx.Member;
		MemberTypes enumValue = member.MemberType;

		foreach (MemberTypes flag in enumValue.EnumerateIntFlags())
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
	/// Tries to get a setter for the field or property that a lambda expression's body accesses.
	/// </summary>
	/// <param name="expression">The lambda expression to read.</param>
	/// <param name="setter">
	/// When this method returns <see langword="true"/>, a <see cref="FieldOrPropertyInfo"/> for the field or property
	/// that the body accesses. When it returns <see langword="false"/>, a <see cref="FieldOrPropertyInfo"/> for a
	/// field or property that can't be set, or <see langword="null"/> when the body isn't a field or property access.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the body accesses a field or property that can be set; otherwise,
	/// <see langword="false"/>.
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
		FlagEnumerator<MemberTypes, int> enumerator = new(enumValue);

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