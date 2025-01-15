using Declaro.Net.Connection;
using Declaro.Net.Test.Helpers;
using Declaro.Net.Test.HttpServiceTests.Base;
using Declaro.Net.Test.TestDataTypes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using System.Net;

namespace Declaro.Net.Test.HttpServiceTests
{
    public class HttpServiceDeleteTests : HttpServiceTestBase
    {
        [Fact]
        public async Task DeleteAsync_PassRequestObject()
        {
            // Arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            var mockHttpClientFactory = new MockHttpClientFactory(mockHttpMessageHandler, "http://127.0.0.1/");
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Logger<HttpService>(LoggerFactory.Create(configure => { }));
            var httpService = new HttpService(logger, mockHttpClientFactory, memoryCache);
            var expectedUri = "api/weather?force=true";

            mockHttpMessageHandler.Expect(HttpMethod.Delete, $"http://127.0.0.1/{expectedUri}").Respond(HttpStatusCode.OK);

            // Act
            await httpService.DeleteAsync(
                new WeatherRequest()
                {
                    City = "Budapest",
                    Date = "2023-09-22"
                },
                queryParameters: ("force","true") );

            // Assert
            mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        }
    }
}
