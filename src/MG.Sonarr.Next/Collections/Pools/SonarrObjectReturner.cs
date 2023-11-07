using MG.Sonarr.Next.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Collections.Pools
{
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

    file sealed class SonarrObjectReturner : IPoolReturner
    {
        readonly Dictionary<Type, IObjectPoolReturnable> _dict;

        public SonarrObjectReturner(IEnumerable<IObjectPoolReturnable> returnables)
        {
            returnables ??= Enumerable.Empty<IObjectPoolReturnable>();

            _dict = new(returnables.TryGetNonEnumeratedCount(out int count) ? count : 0);
            foreach (IObjectPoolReturnable pool in returnables)
            {
                _dict.Add(pool.ReturnsType, pool);
            }
        }

        public void Return(object? obj)
        {
            if (obj is null)
            {
                Debug.Fail("Should this really be null?");
                return;
            }

            if (!_dict.TryGetValue(obj.GetType(), out IObjectPoolReturnable? pool))
            {
                Debug.Fail("Couldn't find an object returner.");
                return;
            }

            pool.Return(obj);
        }
        public void Return(ReadOnlySpan<object> span)
        {
            Guard.IsSpan(span);

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

    public static class ObjectReturnerDepedencyInjection
    {
        public static IServiceCollection AddObjectPoolReturner(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            return services.AddSingleton<IPoolReturner, SonarrObjectReturner>();
        }
    }
}
