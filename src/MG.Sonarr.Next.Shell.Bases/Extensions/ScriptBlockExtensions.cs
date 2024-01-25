using System.Collections.ObjectModel;
using System.Management.Automation.Language;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class ScriptBlockExtensions
    {
        const string UNDERSCORE = "_";
        const string THIS = "this";

        /// <summary>
        /// A custom <see cref="ScriptBlock"/> invocation method that injects a stated object for '$_' and sets the 
        /// scriptblock's global 'ErrorActionPreference' to the specified value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="scriptBlock"></param>
        /// <param name="value"></param>
        /// <param name="errorAction">
        ///     The optional ErrorActionPreference that the executing scriptblock will have set. If this is
        ///     <see langword="null"/>, then the global ActionPreference will be used.
        /// </param>
        /// <returns></returns>
        public static TOutput? InvokeWith<T, TOutput>(this ScriptBlock scriptBlock, T value, ActionPreference? errorAction) where T : notnull
        {
            ArgumentNullException.ThrowIfNull(scriptBlock);
            ArgumentNullException.ThrowIfNull(value);
            int listCount = 2;
            if (errorAction.HasValue)
            {
                listCount++;
            }

            List<PSVariable> variables = new(listCount)
            {
                 new PSVariable(UNDERSCORE, value),
                 new PSVariable(THIS, value),
            };

            if (errorAction.HasValue)
            {
                variables.Add(new PSVariable("ErrorActionPreference", errorAction.Value));
            }

            Collection<PSObject> results = scriptBlock.InvokeWithContext(null, variables, Array.Empty<object>());

            return results.Count > 0 && results[0].ImmediateBaseObject is TOutput tRes
                ? tRes
                : default;
        }

        public static bool IsProperScriptBlock(this ScriptBlock scriptBlock)
        {
            ArgumentNullException.ThrowIfNull(scriptBlock);

            if (scriptBlock.Ast is not ScriptBlockAst scriptAst)
            {
                return false;
            }

            if (scriptAst.BeginBlock is not null || scriptAst.ProcessBlock is not null)
            {
                return true;
            }
            else if (scriptAst.EndBlock is null)
            {
                return false;
            }

            ReadOnlyCollection<StatementAst> statements = scriptAst.EndBlock.Statements;
            if (statements.Count <= 0 || statements[0] is not PipelineAst firstPipeline)
            {
                return false;
            }

            return firstPipeline.PipelineElements.Count > 0
                   &&
                   firstPipeline.PipelineElements[0] is CommandExpressionAst;
        }
    }
}
