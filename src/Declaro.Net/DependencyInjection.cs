using Declaro.Net.Connection;
using Microsoft.Extensions.DependencyInjection;

namespace Declaro.Net
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddHttpService(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<HttpService>();
            return serviceCollection;
        }
    }
}
