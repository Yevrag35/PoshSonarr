using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Extensions
{
    public static class PSObjectExtensions
    {
        public static void AddFromObject<T>(this PSObject pso, T obj, 
            params Expression<Func<T, object>>[] memberExpressions)
        {
            if (memberExpressions == null || memberExpressions.Length <= 0)
                return;

            foreach (Expression<Func<T, object>> ex in memberExpressions)
            {
                pso.Properties.Add<T, object>(obj, ex);
            }
        }

        internal static void Add<T1, T2>(this PSMemberInfoCollection<PSPropertyInfo> props,
            T1 obj, Expression<Func<T1, T2>> memberExpression)
        {
            MemberInfo mi = null;
            if (memberExpression.Body is MemberExpression memEx)
            {
                mi = memEx.Member;
            }
            else if (memberExpression.Body is UnaryExpression unEx && unEx.Operand is MemberExpression unExMem)
            {
                mi = unExMem.Member;
            }

            if (mi != null)
            {
                Func<T1, T2> func = memberExpression.Compile();
                props.Add(new PSNoteProperty(mi.Name, func(obj)));
            }
        }

        private static void Add(this PSMemberInfoCollection<PSPropertyInfo> props, string name, object value)
        {
            props.Add(new PSNoteProperty(name, value));
        }

        public static PSObject ToPSObject(this Hashtable ht)
        {
            if (ht == null || ht.Count <= 0)
                return null;

            var pso = new PSObject();
            foreach (DictionaryEntry kvp in ht)
            {
                pso.Properties.Add(Convert.ToString(kvp.Key), kvp.Value);
            }
            return pso;
        }
    }
}
