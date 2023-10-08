using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Collections
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
                return;
            }

            if (_dict.TryGetValue(obj.GetType(), out var pool))
            {
                pool.Return(obj);
            }
        }
        public void Return(ReadOnlySpan<object> span)
        {
            if (span.IsEmpty)
            {
                return;
            }

            foreach (object? obj in span)
            {
                this.Return(obj);
            }
        }
    }

    public static class ObjectReturnerDepedencyInjection
    {
        public static IServiceCollection AddObjectPoolReturner(this IServiceCollection services)
        {
            return services.AddSingleton<IPoolReturner, SonarrObjectReturner>();
        }
    }
}
