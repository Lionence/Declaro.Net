using Declaro.Net.Connection;
using Declaro.Net.Test.HttpServiceTests.Base;
using Declaro.Net.Test.TestDataTypes;
using Microsoft.Extensions.Caching.Memory;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http.Json;

namespace Declaro.Net.Test.HttpServiceTests
{
    public class HttpServiceListTests
    {
        protected string _ExpectedUri => "api/weather";

        public HttpServiceListTests() { }

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
            var listResponse = new List<WeatherRequestResponse>();
            listResponse.Add(new WeatherRequestResponse() { Celsius = 10, City = "Budapest", Date = "2023-09-20"});
            listResponse.Add(new WeatherRequestResponse() { Celsius = 10, City = "Budapest", Date = "2023-09-22"});
            listResponse.Add(new WeatherRequestResponse() { Celsius = 10, City = "Budapest", Date = "2023-09-25"});

            var mock = new MockHttpMessageHandler();
            mock.When(HttpMethod.Post, $"http://127.0.0.1/{_ExpectedUri}")
                .Respond(HttpStatusCode.OK, JsonContent.Create(listResponse));
            var client = new HttpClient(mock);
            client.BaseAddress = new Uri("http://127.0.0.1");
            var httpService = new HttpService(client, new MemoryCache(new MemoryCacheOptions()));

            // Act
            var response = await httpService.ListAsync(listRequest);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(3, response.Count);
            Assert.NotStrictEqual(listResponse[0], response.ElementAt(0));
            Assert.NotStrictEqual(listResponse[1], response.ElementAt(1));
            Assert.NotStrictEqual(listResponse[2], response.ElementAt(2));
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
            var listResponse = new List<WeatherResponse>();
            listResponse.Add(new WeatherResponse() { Celsius = 10, City = "Budapest"});
            listResponse.Add(new WeatherResponse() { Celsius = 11, City = "Budapest"});
            listResponse.Add(new WeatherResponse() { Celsius = 12, City = "Budapest"});

            var mock = new MockHttpMessageHandler();
            mock.When(HttpMethod.Post, $"http://127.0.0.1/{_ExpectedUri}")
                .Respond(HttpStatusCode.OK, JsonContent.Create(listResponse));
            var client = new HttpClient(mock);
            client.BaseAddress = new Uri("http://127.0.0.1");
            var httpService = new HttpService(client, new MemoryCache(new MemoryCacheOptions()));

            // Act
            var response = await httpService.ListAsync<WeatherResponse, WeatherRequest>(listRequest);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(3, response.Count);
            Assert.NotStrictEqual(listResponse[0], response.ElementAt(0));
            Assert.NotStrictEqual(listResponse[1], response.ElementAt(1));
            Assert.NotStrictEqual(listResponse[2], response.ElementAt(2));
        }
    }
}
