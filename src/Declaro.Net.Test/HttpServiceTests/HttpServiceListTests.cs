using Declaro.Net.Connection;
using Declaro.Net.Test.Helpers;
using Declaro.Net.Test.TestDataTypes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http.Json;

namespace Declaro.Net.Test.HttpServiceTests
{
    public class HttpServiceListTests
    {
        [Fact]
        public async Task ListAsync_SameType()
        {
            // Arrange
            var listRequest = new WeatherRequestResponse()
            {
                Celsius = 10,
                City = "Budapest",
                Date = null,
            };
            var listResponse = new List<WeatherRequestResponse>
            {
                new WeatherRequestResponse() { Celsius = 10, City = "Budapest", Date = "2023-09-20" },
                new WeatherRequestResponse() { Celsius = 10, City = "Budapest", Date = "2023-09-22" },
                new WeatherRequestResponse() { Celsius = 10, City = "Budapest", Date = "2023-09-25" }
            };
            var expectedUri = "api/weather?Disctrict=13";

            var mockHttpMessageHandler = new MockHttpMessageHandler();
            mockHttpMessageHandler.Expect(HttpMethod.Get, $"http://127.0.0.1/{expectedUri}")
                .Respond(HttpStatusCode.OK, JsonContent.Create(listResponse));
            var factory = new MockHttpClientFactory(mockHttpMessageHandler, "http://127.0.0.1/");
            var logger = new Logger<HttpService>(LoggerFactory.Create(configure => { }));
            var httpService = new HttpService(logger, factory, new MemoryCache(new MemoryCacheOptions()));

            // Act
            var response = await httpService.ListAsync(listRequest, queryParameters: ("Disctrict", "13"));

            // Assert
            Assert.NotNull(response);
            Assert.Equal(3, response.Count);
            Assert.NotStrictEqual(listResponse[0], response.ElementAt(0));
            Assert.NotStrictEqual(listResponse[1], response.ElementAt(1));
            Assert.NotStrictEqual(listResponse[2], response.ElementAt(2));

            mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_DifferentType()
        {
            // Arrange
            var listRequest = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2022-09-27",
            };
            var listResponse = new List<WeatherResponse>
            {
                new WeatherResponse() { Celsius = 10, City = "Budapest" },
                new WeatherResponse() { Celsius = 11, City = "Budapest" },
                new WeatherResponse() { Celsius = 12, City = "Budapest" }
            };
            var expectedUri = "api/weather?Disctrict=13";

            var mockHttpMessageHandler = new MockHttpMessageHandler();
            mockHttpMessageHandler.Expect(HttpMethod.Get, $"http://127.0.0.1/{expectedUri}")
                .Respond(HttpStatusCode.OK, JsonContent.Create(listResponse));
            var factory = new MockHttpClientFactory(mockHttpMessageHandler, "http://127.0.0.1/");
            var logger = new Logger<HttpService>(LoggerFactory.Create(configure => { }));
            var httpService = new HttpService(logger, factory, new MemoryCache(new MemoryCacheOptions()));

            // Act
            var response = await httpService.ListAsync<WeatherResponse, WeatherRequest>(listRequest, queryParameters: ("Disctrict", "13"));

            // Assert
            Assert.NotNull(response);
            Assert.Equal(3, response.Count);
            Assert.NotStrictEqual(listResponse[0], response.ElementAt(0));
            Assert.NotStrictEqual(listResponse[1], response.ElementAt(1));
            Assert.NotStrictEqual(listResponse[2], response.ElementAt(2));

            mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        }
    }
}
