using Declaro.Net.Connection;
using Declaro.Net.Test.HttpServiceTests.Base;
using Declaro.Net.Test.TestDataTypes;
using RichardSzalay.MockHttp;
using System.Net.Http.Json;
using System.Net;
using Declaro.Net.Test.Helpers;
using Microsoft.Extensions.Caching.Memory;

namespace Declaro.Net.Test.HttpServiceTests
{
    public class HttpServicePutTests : HttpServiceTestBase
    {
        [Fact]
        public async Task PutAsync_PassRequestObject_SameRequestResponse()
        {
            // Arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            var mockHttpClientFactory = new MockHttpClientFactory(mockHttpMessageHandler, "http://127.0.0.1/");
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var httpService = new HttpService(mockHttpClientFactory, memoryCache);
            var expectedUri = "api/weather?Disctrict=13";

            mockHttpMessageHandler
                .Expect(HttpMethod.Put, $"http://127.0.0.1/{expectedUri}").Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherRequestResponse() { Celsius = 10, Date = "2023-09-22", City = "Budapest" }));

            var requestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2023-09-22"
            };

            // Act
            var response = await httpService.PutAsync<WeatherResponse, WeatherRequest>(requestData, queryParameters: ("Disctrict", "13"));

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Budapest", response.City);
            Assert.Equal(10, response.Celsius);

            mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task PutAsync_PassRequestObject_DifferentRequestResponse()
        {
            // Arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            var mockHttpClientFactory = new MockHttpClientFactory(mockHttpMessageHandler, "http://127.0.0.1/");
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var httpService = new HttpService(mockHttpClientFactory, memoryCache);
            var expectedUri = "api/weather?Disctrict=13";

            mockHttpMessageHandler
                .Expect(HttpMethod.Put, $"http://127.0.0.1/{expectedUri}").Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherRequestResponse() { Celsius = 10, Date = "2023-09-22", City = "Budapest" }));

            var requestData = new WeatherRequestResponse()
            {
                City = "Budapest",
                Date = "2023-09-22",
                Celsius = 10
            };

            // Act
            var response = await httpService.PutAsync<WeatherRequestResponse>(requestData, queryParameters: ("Disctrict", "13"));

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Budapest", response.City);
            Assert.Equal("2023-09-22", response.Date);
            Assert.Equal(10, response.Celsius);

            mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        }
    }
}
