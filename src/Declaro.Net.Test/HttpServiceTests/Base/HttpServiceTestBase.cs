using Declaro.Net.Connection;
using Microsoft.Extensions.Configuration;

namespace Declaro.Net.Test.HttpServiceTests.Base
{
    public abstract class HttpServiceTestBase
    {
        protected readonly Type _HttpServiceType = typeof(HttpService);
        protected readonly IConfiguration TestConfiguration;

        protected HttpServiceTestBase()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Endpoints:WeatherResponse", "api/weather?City={0}&Date={1}" },
            };

            TestConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }
    }
}
