using MG.Posh.Extensions.Bound;
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
        #region PARAMETERS
        /// <summary>
        /// A LINQ method for performing a "ContainsKey" lookup on the current <see cref="PSCmdlet"/>'s bound parameters.  The parameter
        /// expression should point to a defined parameter property on the cmdlet.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="PSCmdlet"/> to check on.</typeparam>
        /// <typeparam name="U">The member type from the expression.</typeparam>
        /// <param name="cmdlet">The <see cref="PSCmdlet"/> whose bound parameters will be checked.</param>
        /// <param name="cmdletParameterExpression">
        ///     The expression targeting the specified <see cref="PSCmdlet"/> whose property will be checked in the
        ///     bound parameters dictionary.
        /// </param>
        private bool HasParameterSpecified<T, U>(T cmdlet, Expression<Func<T, U>> cmdletParameterExpression) where T : PSCmdlet
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

        internal string GetNameFromExpression<T>(Expression<Func<T, object>> propExpression) where T : PSCmdlet
        {
            if (propExpression.Body is MemberExpression memEx)
            {
                return memEx.Member.Name;
            }
            else if (propExpression.Body is UnaryExpression une && une.Operand is MemberExpression unMemEx)
            {
                return unMemEx.Member.Name;
            }
            else
            {
                return null;
            }
        }

        //public bool HasAllParametersSpecified<T>(T cmdlet, params Expression<Func<T, object>>[] cmdletParameterExpressions) where T : PSCmdlet
        //{
        //    bool result = false;
        //    if (cmdletParameterExpressions != null && cmdletParameterExpressions.Length > 0)
        //    {
        //        result = SelectAllParameterNames(cmdletParameterExpressions)
        //            .All(x => cmdlet.MyInvocation.BoundParameters.ContainsKey(x));
        //    }
        //    return result;
        //}
        //public bool HasAnyParameterSpecified<T>(T cmdlet, params Expression<Func<T, object>>[] cmdletParameterExpressions) where T : PSCmdlet
        //{
        //    bool result = false;
        //    if (cmdletParameterExpressions != null && cmdletParameterExpressions.Length > 0)
        //    {
        //        result = SelectAllParameterNames(cmdletParameterExpressions)
        //            .Any(x => cmdlet.MyInvocation.BoundParameters.ContainsKey(x));
        //    }
        //    return result;
        //}
        //public bool HasNoneOfTheParametersSpecified<T>(T cmdlet, params Expression<Func<T, object>>[] cmdletParameterExpressions) where T : PSCmdlet
        //{
        //    bool result = false;
        //    if (cmdletParameterExpressions != null || cmdletParameterExpressions.Length > 0)
        //    {
        //        result = SelectAllParameterNames(cmdletParameterExpressions)
        //            .All(x => ! cmdlet.MyInvocation.BoundParameters.ContainsKey(x));
        //    }
        //    return result;
        //}
        //private IEnumerable<string> SelectAllParameterNames<T>(IEnumerable<Expression<Func<T, object>>> cmdletParameterExpressions) where T : PSCmdlet
        //{
        //    foreach (Expression<Func<T, object>> expression in cmdletParameterExpressions)
        //    {
        //        if (expression.Body is MemberExpression memEx)
        //        {
        //            yield return memEx.Member.Name;
        //        }
        //        else if (expression.Body is UnaryExpression unEx && unEx.Operand is MemberExpression unMemEx)
        //        {
        //            yield return unMemEx.Member.Name;
        //        }
        //    }
        //}

        #endregion

        #region WILDCARD FILTERING
        protected List<T> FilterByMultipleStrings<T>(List<T> listOfItems, IEnumerable<string> wildcardContainingStrings,
            params Expression<Func<T, string>>[] expressions) where T : IJsonResult
        {
            if (listOfItems != null && listOfItems.Count > 0)
            {
                var masterList = new List<T>(listOfItems.Count);
                IEnumerable<WildcardPattern> patterns = wildcardContainingStrings
                    .Select(s => new WildcardPattern(s, WildcardOptions.IgnoreCase));

                Func<T, string>[] funcs = this.CompileFuncs(expressions);

                return listOfItems.FindAll(x =>
                    funcs.Any(f => patterns.Any(pat => pat.IsMatch(f(x)))));
            }
            else
            {
                return listOfItems;
            }
        }

        private Func<T, string>[] CompileFuncs<T>(params Expression<Func<T, string>>[] expressions)
        {
            var funcs = new Func<T, string>[expressions.Length];
            for (int i = 0; i < expressions.Length; i++)
            {
                funcs[i] = expressions[i].Compile();
            }
            return funcs;
        }

        protected List<T> FilterByStrings<T>(List<T> listOfItems, Expression<Func<T, string>> propertyExpressionOfItem, 
            IEnumerable<string> wildcardContainingStrings) where T : IJsonResult
        {
            if (listOfItems != null && wildcardContainingStrings != null && listOfItems.Count > 0 && propertyExpressionOfItem.Body is MemberExpression)
            {
                Func<T, string> propertyFunc = propertyExpressionOfItem.Compile();

                IEnumerable<WildcardPattern> patterns = wildcardContainingStrings
                    .Select(s => new WildcardPattern(s, WildcardOptions.IgnoreCase));

                return listOfItems
                    .FindAll(x =>
                        propertyFunc(x) != null
                        &&
                        patterns
                            .Any(pat =>
                                pat.IsMatch(propertyFunc(x))));
            }
            else
            {
                return listOfItems;
            }
        }

        protected internal IEnumerable<T> FilterByStrings<T>(IEnumerable<T> listOfItems, Expression<Func<T, string>> propertyExpressionOfItem,
            IEnumerable<string> wildcardContainingStrings) where T : IJsonResult
        {
            if (listOfItems != null && wildcardContainingStrings != null && propertyExpressionOfItem.Body is MemberExpression)
            {
                Func<T, string> propertyFunc = propertyExpressionOfItem.Compile();

                IEnumerable<WildcardPattern> patterns = wildcardContainingStrings
                    .Select(s => new WildcardPattern(s, WildcardOptions.IgnoreCase));

                return listOfItems
                    .Where(x =>
                        propertyFunc(x) != null
                        &&
                        patterns
                            .Any(pat =>
                                pat.IsMatch(propertyFunc(x))));
            }
            else
            {
                return listOfItems;
            }
        }

        protected List<T> FilterByStrings<T>(List<T> listOfItems, Expression<Func<T, IEnumerable<string>>> propertyExpressionOfItem,
            IEnumerable<string> wildcardContainingStrings) where T : IJsonResult
        {
            if (listOfItems != null && listOfItems.Count > 0 && propertyExpressionOfItem.Body is MemberExpression)
            {
                Func<T, IEnumerable<string>> propertyFunc = propertyExpressionOfItem.Compile();

                IEnumerable<WildcardPattern> patterns = wildcardContainingStrings
                    .Select(s => new WildcardPattern(s, WildcardOptions.IgnoreCase));

                return listOfItems
                    .FindAll(x =>
                        propertyFunc(x) != null
                        &&
                        patterns
                            .Any(pat =>
                                propertyFunc(x)
                                    .Any(s =>
                                        pat.IsMatch(s))));
            }
            else
            {
                return listOfItems;
            }
        }

        /// <summary>
        /// Takes a list of <typeparamref name="T"/> and compares the <see cref="string"/> property value of each element
        /// against the specified <see cref="BaseSonarrCmdlet"/>'s parameter which accepts wildcard input.  If the parameter is found not to 
        /// have been specified, the entire list is returned.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/>-implementing class that the <see cref="List{T}"/> is comprised of.</typeparam>
        /// <typeparam name="U">The <see cref="BaseSonarrCmdlet"/> that the parameter is a property of.</typeparam>
        /// <param name="listOfItems">The source list of items that will be filtered based on matching criteria.</param>
        /// <param name="propertyExpressionOfItem">
        ///     The member <see cref="Expression"/> of <typeparamref name="T"/>.  The subsequent string value of which, 
        ///     will be put through the <see cref="WildcardPattern"/>s to see if it matches.
        /// </param>
        /// <param name="cmdlet">The <see cref="BaseSonarrCmdlet"/> the reference parameter values will be pulled from.</param>
        /// <param name="parameterExpression">
        /// The member <see cref="Expression"/> of the cmdlet's parameter.  The <see cref="string"/>
        /// values will be the wildcard references.
        /// </param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="NotImplementedException"/>
        /// <returns>A <see cref="List{T}"/> of the filtered objects that matched through wildcards.</returns>
        protected List<T> FilterByStringParameter<T, U>(List<T> listOfItems, Expression<Func<T, string>> propertyExpressionOfItem,
            U cmdlet, Expression<Func<U, IEnumerable<string>>> parameterExpression)
            where T : class, IJsonResult where U : BaseSonarrCmdlet
        {
            if (listOfItems != null && listOfItems.Count > 0
                && this.HasParameterSpecified(cmdlet, parameterExpression)
                && propertyExpressionOfItem.Body is MemberExpression)
            {
                Func<U, IEnumerable<string>> cmdletFunc = parameterExpression.Compile();
                Func<T, string> propertyFunc = propertyExpressionOfItem.Compile();

                IEnumerable<string> propVal = cmdletFunc(cmdlet);

                IEnumerable<WildcardPattern> patterns = propVal
                    .Select(s => new WildcardPattern(s, WildcardOptions.IgnoreCase));

                return listOfItems
                    .FindAll(x => patterns
                        .Any(pat => pat
                            .IsMatch(propertyFunc(x))));
            }
            else
            {
                return listOfItems;
            }
        }

        /// <summary>
        /// Takes a list of <typeparamref name="T"/> and compares the <see cref="string"/> property value of each element
        /// against the specified <see cref="BaseSonarrCmdlet"/>'s parameter which accepts wildcard input.  If the parameter is found not to 
        /// have been specified, the entire list is returned.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/>-implementing class that the <see cref="List{T}"/> is comprised of.</typeparam>
        /// <typeparam name="U">The <see cref="BaseSonarrCmdlet"/> that the parameter is a property of.</typeparam>
        /// <param name="listOfItems">The source list of items that will be filtered based on matching criteria.</param>
        /// <param name="propertyExpressionOfItem">
        ///     The member <see cref="Expression"/> of <typeparamref name="T"/>.  The subsequent string value of which, 
        ///     will be put through the <see cref="WildcardPattern"/>s to see if it matches.
        /// </param>
        /// <param name="cmdlet">The <see cref="BaseSonarrCmdlet"/> the reference parameter values will be pulled from.</param>
        /// <param name="parameterExpression">
        /// The member <see cref="Expression"/> of the cmdlet's parameter.  The <see cref="string"/>
        /// values will be the wildcard references.
        /// </param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="NotImplementedException"/>
        /// <returns>A <see cref="List{T}"/> of the filtered objects that matched through wildcards.</returns>
        protected IEnumerable<T> FilterByStringParameter<T, U>(IEnumerable<T> listOfItems, Expression<Func<T, string>> propertyExpressionOfItem,
            U cmdlet, Expression<Func<U, IEnumerable<string>>> parameterExpression)
            where T : class, IJsonResult where U : BaseSonarrCmdlet
        {
            if (listOfItems != null
                && propertyExpressionOfItem.Body is MemberExpression)
            {
                Func<U, IEnumerable<string>> cmdletFunc = parameterExpression.Compile();
                Func<T, string> propertyFunc = propertyExpressionOfItem.Compile();

                IEnumerable<string> propVal = cmdletFunc(cmdlet);

                IEnumerable<WildcardPattern> patterns = propVal
                    .Select(s => new WildcardPattern(s, WildcardOptions.IgnoreCase));

                return listOfItems
                    .Where(x => patterns
                        .Any(pat => pat
                            .IsMatch(propertyFunc(x))));
            }
            else
            {
                return listOfItems;
            }
        }

        /// <summary>
        /// Takes a list of <typeparamref name="T"/> and compares the <see cref="IEnumerable{T}"/> (where T is <see cref="string"/>) property values of each element
        /// against the specified <see cref="BaseSonarrCmdlet"/>'s parameter which accepts wildcard input.  If the parameter is found not to have
        /// been specified, the entire list is returned.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/>-implementing class that the <see cref="List{T}"/> is comprised of.</typeparam>
        /// <typeparam name="U">The <see cref="BaseSonarrCmdlet"/> that the parameter is a property of.</typeparam>
        /// <param name="listOfItems">The source collection of items that will be filtered based on matching criteria.</param>
        /// <param name="propertyExpressionOfListItem">
        ///     The member <see cref="Expression"/> of <typeparamref name="T"/>.  The subsequent string value of which, 
        ///     will be put through the <see cref="WildcardPattern"/>s to see if it matches.
        /// </param>
        /// <param name="cmdlet">The <see cref="BaseSonarrCmdlet"/> the reference parameter values will be pulled from.</param>
        /// <param name="parameterExpression">
        /// The member <see cref="Expression"/> of the cmdlet's parameter.  The <see cref="string"/>
        /// values will be the wildcard references.
        /// </param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="NotImplementedException"/>
        /// <returns>A <see cref="List{T}"/> of the filtered objects that matched through wildcards.</returns>
        protected IEnumerable<T> FilterByStringParameter<T, U>(IEnumerable<T> listOfItems, 
            Expression<Func<T, IEnumerable<string>>> propertyExpressionOfListItem,
            U cmdlet, Expression<Func<U, IEnumerable<string>>> parameterExpression) where T : IJsonResult where U : BaseSonarrCmdlet
        {
            if (listOfItems != null && this.HasParameterSpecified(cmdlet, parameterExpression))
            {
                Func<U, IEnumerable<string>> cmdletFunc = parameterExpression.Compile();
                Func<T, IEnumerable<string>> propertyFunc = propertyExpressionOfListItem.Compile();

                IEnumerable<string> parameterVal = cmdletFunc(cmdlet);

                IEnumerable<WildcardPattern> patterns = parameterVal
                    .Select(s => new WildcardPattern(s, WildcardOptions.IgnoreCase));

                return listOfItems
                    .Where(x =>
                        propertyFunc(x) != null
                        &&
                        patterns
                            .Any(pat =>
                                propertyFunc(x)
                                    .Any(s =>
                                        pat.IsMatch(s))));
            }
            else
            {
                return listOfItems;
            }
        }

        #endregion
    }
}
