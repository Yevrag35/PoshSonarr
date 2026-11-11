using System.Collections.ObjectModel;
using System.Management.Automation.Language;

namespace MG.Sonarr.Next.Extensions.Pwsh;

/// <summary>
/// Provides extension methods for <see cref="ScriptBlock"/> objects to facilitate custom invocation and validation in the context of PowerShell cmdlets.
/// </summary>
internal static class ScriptBlockExtensions
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
		/// A custom <see cref="ScriptBlock"/> invocation method that injects a stated object for '$_' and '$this', and
		/// defines '$ErrorActionPreference' in the script block's scope with the specified value.
		/// </summary>
		/// <typeparam name="T">The type of the input value to inject into the script block.</typeparam>
		/// <typeparam name="TOutput">The type of the output value expected from the script block.</typeparam>
		/// <param name="value">The input value to inject into the script block.</param>
		/// <param name="errorAction">
		///     The optional ErrorActionPreference that the executing scriptblock will have set. If this is
		///     <see langword="null"/>, then the script block reads the preference from the scopes that it runs in.
		/// </param>
		/// <returns>
		/// The script block's first output when it's a <typeparamref name="TOutput"/>; otherwise, <see langword="default"/>,
		/// as when the script block writes nothing or writes <see langword="null"/> first.
		/// </returns>
		internal TOutput? InvokeWith<T, TOutput>(T value, ActionPreference? errorAction) where T : notnull
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

			// A script block that writes $null puts a null element in the results.
			return results.Count > 0 && results[0]?.ImmediateBaseObject is TOutput tRes
				? tRes
				: default;
		}

		/// <summary>
		/// Determines whether the given <see cref="ScriptBlock"/> is a "proper" scriptblock, meaning it has either a Begin or Process block, or an End block whose first statement is a pipeline that starts with a command expression.
		/// </summary>
		/// <remarks>
		/// It tells a script, such as <c>$_.Label + '-old'</c>, from text that parses as a command name, such as <c>mytag</c>,
		/// whose first pipeline element is a command rather than a command expression.
		/// </remarks>
		/// <returns><see langword="true"/> if the script block is proper; otherwise, <see langword="false"/>.</returns>
		internal bool IsProperScriptBlock()
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
