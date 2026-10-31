using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Collections.Pools;

/// <summary>
/// An interface exposing methods for returning objects back into their respective
/// <see cref="IObjectPool{T}"/>.
/// </summary>
public interface IPoolReturner
{
	/// <summary>
	/// Returns a single object back into its <see cref="IObjectPool{T}"/>.
	/// </summary>
	/// <param name="obj">The object to return.</param>
	void Return(object? obj);
	/// <summary>
	/// Returns a span of objects back into their respective <see cref="IObjectPool{T}"/> instances.
	/// </summary>
	/// <param name="span">The span of objects to return.</param>
	void Return(ReadOnlySpan<object> span);
}

/// <summary>
/// Represents the default implementation that routes objects to their matching object pools.
/// </summary>
internal sealed class SonarrObjectReturner : IPoolReturner
{
	readonly Dictionary<Type, IObjectPoolReturnable> _dict;

	/// <summary>
	/// Initializes a new <see cref="SonarrObjectReturner"/> instance.
	/// </summary>
	/// <param name="returnables">The pool return handlers keyed by return type.</param>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="returnables"/> is null.</exception>
	public SonarrObjectReturner(IEnumerable<IObjectPoolReturnable> returnables)
	{
		ArgumentNullException.ThrowIfNull(returnables);

		_dict = returnables.TryGetNonEnumeratedCount(out int count)
			? new(count)
			: [];

		foreach (IObjectPoolReturnable pool in returnables)
		{
			_dict.Add(pool.ReturnsType, pool);
		}
	}

	/// <inheritdoc cref="IPoolReturner.Return(object)"/>
	public void Return(object? obj)
	{
		if (obj is null || !_dict.TryGetValue(obj.GetType(), out IObjectPoolReturnable? pool))
		{
			Debug.Fail("Should this really be null?");
			return;
		}

		pool.Return(obj);
	}

	/// <inheritdoc cref="IPoolReturner.Return(ReadOnlySpan{object})"/>
	public void Return(ReadOnlySpan<object> span)
	{
		if (span.IsEmpty)
		{
			Debug.Fail("Should this really be empty?");
			return;
		}

		foreach (object? obj in span)
		{
			Debug.Assert(obj is not null);
			this.Return(obj);
		}
	}
}

/// <summary>
/// Provides dependency injection registrations for object pool return services.
/// </summary>
public static class ObjectReturnerDepedencyInjection
{
	/// <summary>
	/// Adds the object pool returner service to the specified service collection.
	/// </summary>
	/// <param name="services">The service collection to update.</param>
	/// <returns>The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
	public static IServiceCollection AddObjectPoolReturner(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		return services.AddSingleton<IPoolReturner, SonarrObjectReturner>();
	}
}
