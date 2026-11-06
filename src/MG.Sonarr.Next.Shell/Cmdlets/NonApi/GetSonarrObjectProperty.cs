using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.NonApi;

/// <summary>
/// Represents the <c>Get-SonarrObjectProperty</c> cmdlet, which lists the properties of objects, such as the ones that
/// the module's cmdlets write.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet sends nothing to Sonarr. It collects the properties of every object from the pipeline or from
/// <c>-InputObject</c>, and once the input ends, writes each one as a <see cref="SonarrProperty"/>, with its name, the
/// type of its value, its current value, whether it's read-only, and the type of the object that it belongs to.
/// </para>
/// <para>
/// The properties are sorted by the object's type and then by name, ignoring case, and a property that several objects
/// of the same type share is written once, with the value of the first such object. Only properties that can be set
/// are listed, unless <c>-IncludeReadOnly</c> is present.
/// </para>
/// <para>
/// The cmdlet's aliases are <c>Get-SonarrObjectProperties</c> and <c>Get-SonarrProperties</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrObjectProperty")]
[Alias("Get-SonarrObjectProperties", "Get-SonarrProperties")]
public sealed class GetSonarrObjectProperty : SonarrCmdletBase
{
	private SortedSet<SonarrProperty> _set = null!;

	/// <summary>
	/// Gets or sets the objects whose properties the cmdlet lists.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input.
	/// </remarks>
	/// <value>The objects. The default is an empty array, for no properties.</value>
	[Parameter(Position = 0, ValueFromPipeline = true)]
	public PSObject[] InputObject { get; set; } = Array.Empty<PSObject>();

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet also lists the properties that can't be set.
	/// </summary>
	/// <value>The switch, which is present to include read-only properties.</value>
	[Parameter]
	public SwitchParameter IncludeReadOnly { get; set; }

	/// <summary>
	/// Creates the set that collects the properties.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		_set = [];
	}

	/// <summary>
	/// Adds the properties of each object in <c>-InputObject</c> to the set of properties to write.
	/// </summary>
	/// <remarks>
	/// The object's type is the PowerShell name of its .NET type, such as
	/// <c>MG.Sonarr.Next.Models.Series.SeriesObject</c>, so the properties of an object that isn't one of the module's
	/// models, such as a custom object, are listed under the name of the <see cref="PSObject"/> that wraps it.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		if (this.InputObject.Length <= 0)
		{
			return;
		}

		bool includeRo = this.IncludeReadOnly.ToBool();

		foreach (PSObject pso in this.InputObject)
		{
			string objectType = pso.GetType().GetPSTypeName(removeBrackets: true);
			foreach (PSPropertyInfo property in pso.Properties)
			{
				if (includeRo || property.IsSettable)
				{
					_set.Add(new()
					{
						CurrentValue = property.Value,
						IsReadOnly = !property.IsSettable,
						Name = property.Name,
						ObjectType = objectType,
						Type = property.TypeNameOfValue,
					});
				}
			}
		}
	}

	/// <summary>
	/// Writes the collected properties to the pipeline, in their sorted order.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		if (_set.Count <= 0)
		{
			return;
		}

		this.WriteCollection(_set);
	}
}

