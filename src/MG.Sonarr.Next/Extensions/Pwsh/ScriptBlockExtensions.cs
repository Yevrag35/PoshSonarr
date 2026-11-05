using System.Collections.ObjectModel;
using System.Management.Automation.Language;

namespace MG.Sonarr.Next.Extensions.Pwsh;

/// <summary>
/// Provides extension methods for <see cref="ScriptBlock"/> objects to facilitate custom invocation and validation in the context of PowerShell cmdlets.
/// </summary>
public static class ScriptBlockExtensions
{
	private const string UNDERSCORE = "_";
	private const string THIS = "this";

	/// <summary>
	/// An extension method for <see cref="ScriptBlock"/> that allows for custom invocation with a specified input object and optional error action preference.
	/// </summary>
	/// <param name="scriptBlock">The script block to extend.</param>
	extension(ScriptBlock scriptBlock)
	{
		/// <summary>
		/// A custom <see cref="ScriptBlock"/> invocation method that injects a stated object for '$_' and sets the 
		/// scriptblock's global 'ErrorActionPreference' to the specified value.
		/// </summary>
		/// <typeparam name="T">The type of the input value to inject into the script block.</typeparam>
		/// <typeparam name="TOutput">The type of the output value expected from the script block.</typeparam>
		/// <param name="value">The input value to inject into the script block.</param>
		/// <param name="errorAction">
		///     The optional ErrorActionPreference that the executing scriptblock will have set. If this is
		///     <see langword="null"/>, then the global ActionPreference will be used.
		/// </param>
		/// <returns>The output value from the script block, or <see langword="default"/> if no output is produced.</returns>
		public TOutput? InvokeWith<T, TOutput>(T value, ActionPreference? errorAction) where T : notnull
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

			Collection<PSObject> results = scriptBlock.InvokeWithContext(null, variables, []);

			return results.Count > 0 && results[0].ImmediateBaseObject is TOutput tRes
				? tRes
				: default;
		}

		/// <summary>
		/// Determines whether the given <see cref="ScriptBlock"/> is a "proper" scriptblock, meaning it has either a Begin or Process block, or an End block that contains a pipeline with at least one command expression.
		/// </summary>
		/// <returns><see langword="true"/> if the script block is proper; otherwise, <see langword="false"/>.</returns>
		public bool IsProperScriptBlock()
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

			return scriptAst.EndBlock.Statements is { Count: > 0 } statements
				&& statements[0] is PipelineAst firstPipeline
				&& firstPipeline.PipelineElements is { Count: > 0 } pipeElements
				&& pipeElements[0] is CommandExpressionAst;
		}
	}
}
