using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    internal static class EnumerableExtensions
    {
        internal static IEnumerable<T2> ThenFilterByValues<T1, T2, T3>(this IEnumerable<T2> filterThis,
            T1 cmdlet,
            Expression<Func<T1, object>> parameter,
            Expression<Func<T2, T3>> member,
            IEnumerable<T3> values) where T1 : BaseSonarrCmdlet where T2 : IJsonResult
        {
            if (cmdlet.ContainsParameter(parameter) && filterThis != null && values != null)
            {
                Func<T2, T3> func = member.Compile();
                return filterThis
                    .Where(x =>
                        values.Contains(func(x)));
            }
            return filterThis;
        }

        internal static IEnumerable<T2> ThenFilterByStrings<T1, T2>(this IEnumerable<T2> filterThis,
            T1 cmdlet,
            Expression<Func<T1, object>> parameter,
            Expression<Func<T2, string>> member,
            IEnumerable<string> wildcardStrings) where T1 : BaseSonarrCmdlet where T2 : IJsonResult
        {
            if (cmdlet.ContainsParameter(parameter))
            {
                filterThis = cmdlet.FilterByStrings(filterThis, member, wildcardStrings);
            }
            return filterThis;
        }

        internal static IEnumerable<T2> ThenFilterBy<T1, T2>(this IEnumerable<T2> filterThis, 
            T1 cmdlet, 
            Expression<Func<T1, object>> parameter,
            Expression<Func<T1, bool>> condition,
            Expression<Func<T2, bool>> whereClause) where T1 : BaseSonarrCmdlet where T2 : IJsonResult
        {
            if (cmdlet.ContainsParameter(parameter))
            {
                if (condition != null)
                {
                    Func<T1, bool> func = condition.Compile();
                    if (!func(cmdlet))
                        return filterThis;
                }

                //string paramName = cmdlet.GetNameFromExpression(propExp);

                //if (!cmdlet._isDebugging)
                //    cmdlet.WriteFormatVerbose("Filtering by '{0}'.", paramName);

                filterThis = cmdlet.FilterWhere(filterThis, whereClause);

                //if (cmdlet._isDebugging)
                //{
                //    cmdlet.WriteFormatDebug("The following series names are left after filtering by '{0}':{1}{2}",
                //        paramName,
                //        Environment.NewLine,
                //        string.Join(Environment.NewLine, filterThis.Select(x => x.CleanTitle)));
                //}
                return filterThis;
            }
            else
                return filterThis;
        }
    }
}
