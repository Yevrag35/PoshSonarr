using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MG.Sonarr.Functionality.Extensions
{
    public static class DictionaryExtensions
    {
        public static bool ContainsKey<T>(this Dictionary<string, JToken> dict, Expression<Func<T, object>> propertyExpression)
        {
            if (propertyExpression.Body is MemberExpression memEx)
            {

            }
            else if (propertyExpression.Body is UnaryExpression unEx && unEx.Operand is MemberExpression unExMem)
                return dict.ContainsKey(unExMem.Member.Name);

            else
                return false;
        }
    }
}
