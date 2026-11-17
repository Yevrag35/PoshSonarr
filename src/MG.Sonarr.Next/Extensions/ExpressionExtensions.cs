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
internal static class ExpressionExtensions
{
	/// <summary>
	/// Gets the field or property that a lambda expression's body accesses.
	/// </summary>
	/// <param name="expression">The lambda expression to read.</param>
	/// <returns>
	/// A <see cref="FieldOrPropertyInfo"/> for the field or property that the body accesses, or an empty one, whose
	/// <see cref="FieldOrPropertyInfo.IsEmpty"/> is <see langword="true"/>, when the body isn't a field or property
	/// access.
	/// </returns>
	internal static FieldOrPropertyInfo GetMemberInfo(this LambdaExpression expression)
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
			}
		}

		return default;
	}
}