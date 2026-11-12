namespace MG.Sonarr.Next.Extensions.Pwsh;

/// <summary>
/// Provides helpers that add, replace, convert, and update <see cref="PSObject"/> properties.
/// </summary>
internal static partial class PSOExtensions
{
	/// <summary>
	/// Provides an indexing extension block for the <see cref="PSObject"/> class.
	/// </summary>
	/// <param name="pso">The <see cref="PSObject"/> to extend.</param>
	extension(PSObject pso)
	{
		/// <summary>
		/// Gets the value of a property from a PSObject by its name.
		/// </summary>
		/// <remarks>
		/// Unlike the indexer on <see cref="PSObject.Properties"/>, this indexer returns the underlying value
		/// of the property rather than the <see cref="PSPropertyInfo"/> object itself. Continue to use the
		/// Properties indexer if you need to access the property metadata or manipulate the property itself.
		/// </remarks>
		/// <param name="propertyName">The name of the property to retrieve.</param>
		/// <returns>The value of the property if it exists; otherwise, <see langword="null"/>.</returns>
		/// <inheritdoc cref="PSMemberInfoIntegratingCollection{T}.this[string]" path="/exception"/>
		internal object? this[string propertyName]
		{
			get => pso.Properties[propertyName]?.Value;
			set => pso.Properties[propertyName]?.Value = value;
		}
	}
}
