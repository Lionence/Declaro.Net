using Declaro.Net.Connection;
using Microsoft.Extensions.DependencyInjection;

namespace Declaro.Net
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddHttpService(this IServiceCollection serviceCollection, Func<HttpClient, HttpClient>? configureHttpClient = null)
        {
            var httpclientFactor = new HttpClientFactory(configureHttpClient);
            serviceCollection.AddKeyedSingleton<IHttpClientFactory>("f7b68ed9-749f-4d9f-a537-4416e6084b30_Declaro_HttpClientFactory", httpclientFactor);
            serviceCollection.AddSingleton<HttpService>();
            return serviceCollection;
        }
    }
}
