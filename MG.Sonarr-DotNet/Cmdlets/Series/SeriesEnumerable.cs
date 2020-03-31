using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MG.Sonarr.Cmdlets
{
    internal static class SeriesEnumerable
    {
        internal static IEnumerable<SeriesResult> ThenFilterByStrings(this IEnumerable<SeriesResult> filterThis,
            GetSeries cmdlet,
            Expression<Func<GetSeries, object>> paramExp,
            Expression<Func<SeriesResult, string>> propExp,
            IEnumerable<string> wildcardStrings)
        {
            if (cmdlet.ContainsParameter(paramExp))
            {
                filterThis = cmdlet.FilterByStrings(filterThis, propExp, wildcardStrings);
            }
            return filterThis;
        }

        internal static IEnumerable<SeriesResult> ThenFilterBy(this IEnumerable<SeriesResult> filterThis, 
            GetSeries cmdlet, 
            Expression<Func<GetSeries, object>> propExp,
            Expression<Func<GetSeries, bool>> condition,
            Expression<Func<SeriesResult, bool>> whereClause)
        {
            if (cmdlet.ContainsParameter(propExp))
            {
                if (condition != null)
                {
                    Func<GetSeries, bool> func = condition.Compile();
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
