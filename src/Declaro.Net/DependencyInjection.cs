using Declaro.Net.Connection;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Declaro.Net
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddHttpService(this IServiceCollection serviceCollection, Action<HttpClient>? configureHttpClient = null)
        {
            var httpclientFactor = new HttpClientFactory(configureHttpClient);
            serviceCollection.AddSingleton<IHttpClientFactory>(httpclientFactor);
            serviceCollection.AddSingleton<HttpService>();
            return serviceCollection;
        }
    }
}
