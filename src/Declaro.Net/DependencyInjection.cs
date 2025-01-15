using Declaro.Net.Connection;
using Microsoft.Extensions.DependencyInjection;

namespace Declaro.Net
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddHttpService(this IServiceCollection serviceCollection, Func<HttpClient, HttpClient>? configureHttpClient = null)
        {
            var httpclientFactor = new HttpClientFactory(configureHttpClient);
            serviceCollection.AddKeyedSingleton<IHttpClientFactory>(Constants.HTTPCLIENTFACTORY_DI_KEY, httpclientFactor);
            serviceCollection.AddSingleton<HttpService>();
            return serviceCollection;
        }
    }
}
