using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets
{
    public abstract partial class BaseSonarrCmdlet
    {
        /// <summary>
        /// A LINQ method for performing a "ContainsKey" lookup on the current <see cref="PSCmdlet"/>'s bound parameters.  The parameter
        /// expression should point to a defined parameter property on the cmdlet.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="PSCmdlet"/> to check on.</typeparam>
        /// <param name="cmdlet">The <see cref="PSCmdlet"/> whose bound parameters will be checked.</param>
        /// <param name="cmdletParameterExpression">
        ///     The expression targeting the specified <see cref="PSCmdlet"/> whose property will be checked in the
        ///     bound parameters dictionary.
        /// </param>
        public bool HasParameterSpecified<T>(T cmdlet, Expression<Func<T, object>> cmdletParameterExpression) where T : PSCmdlet
        {
            bool result = false;
            if (cmdletParameterExpression.Body is MemberExpression memEx)
            {
                return cmdlet.MyInvocation.BoundParameters.ContainsKey(memEx.Member.Name);
            }
            else if (cmdletParameterExpression.Body is UnaryExpression une && une.Operand is MemberExpression unMemEx)
            {
                return cmdlet.MyInvocation.BoundParameters.ContainsKey(unMemEx.Member.Name);
            }
            return result;
        }
        public bool HasAllParametersSpecified<T>(T cmdlet, params Expression<Func<T, object>>[] cmdletParameterExpressions) where T : PSCmdlet
        {
            bool result = false;
            if (cmdletParameterExpressions != null || cmdletParameterExpressions.Length > 0)
            {
                result = SelectAllParameterNames(cmdletParameterExpressions)
                    .All(x => cmdlet.MyInvocation.BoundParameters.ContainsKey(x));
            }
            return result;
        }
        public bool HasAnyParameterSpecified<T>(T cmdlet, params Expression<Func<T, object>>[] cmdletParameterExpressions) where T : PSCmdlet
        {
            bool result = false;
            if (cmdletParameterExpressions != null && cmdletParameterExpressions.Length > 0)
            {
                result = SelectAllParameterNames(cmdletParameterExpressions)
                    .Any(x => cmdlet.MyInvocation.BoundParameters.ContainsKey(x));
            }
            return result;
        }
        private IEnumerable<string> SelectAllParameterNames<T>(IEnumerable<Expression<Func<T, object>>> cmdletParameterExpressions) where T : PSCmdlet
        {
            foreach (Expression<Func<T, object>> expression in cmdletParameterExpressions)
            {
                if (expression.Body is MemberExpression memEx)
                {
                    yield return memEx.Member.Name;
                }
                else if (expression.Body is UnaryExpression unEx && unEx.Operand is MemberExpression unMemEx)
                {
                    yield return unMemEx.Member.Name;
                }
            }
        }
    }
}
