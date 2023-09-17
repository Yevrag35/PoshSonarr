namespace MG.Sonarr.Next.Services.Extensions
{
    public static class ExpressionExtensions
    {
        public static bool TryGetAsMember(this LambdaExpression expression, [NotNullWhen(true)] out MemberExpression? memberExpression)
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
    }
}