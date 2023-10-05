using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Services.Collections
{
    public interface IPoolReturner
    {
        void Return(object? obj);
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
