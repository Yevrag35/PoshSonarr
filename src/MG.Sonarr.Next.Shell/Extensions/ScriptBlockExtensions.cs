using System.Collections.ObjectModel;
using System.Management.Automation.Language;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class ScriptBlockExtensions
    {
        const string UNDERSCORE = "_";

        public static TOutput? InvokeWith<T, TOutput>(this ScriptBlock scriptBlock, T value, ActionPreference errorAction) where T : notnull
        {
            ArgumentNullException.ThrowIfNull(scriptBlock);
            ArgumentNullException.ThrowIfNull(value);

            List<PSVariable> variables = new(2)
            {
                 new PSVariable(UNDERSCORE, value),
                 new PSVariable("ErrorActionPreference", errorAction),
            };

            Collection<PSObject> results = scriptBlock.InvokeWithContext(null, variables);

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
