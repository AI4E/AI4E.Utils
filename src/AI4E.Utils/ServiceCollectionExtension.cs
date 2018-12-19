using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace AI4E.Utils
{
    public static partial class ServiceCollectionExtension
    {
        public static T GetService<T>(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var serviceDescriptor = services.LastOrDefault(d => d.ServiceType == typeof(T));

            return (T)serviceDescriptor?.ImplementationInstance;
        }
    }
}
