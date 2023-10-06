using Declaro.Net.Connection;
using Declaro.Net.Test.HttpServiceTests.Base;
using Declaro.Net.Test.TestDataTypes;
using RichardSzalay.MockHttp;
using System.Net.Http.Json;
using System.Net;
using Declaro.Net.Test.Helpers;

namespace Declaro.Net.Test.HttpServiceTests
{
    public class HttpServicePostTests : HttpServiceTestBase
    {
        protected override IHttpClientFactory _HttpClientFactory { get; }
        protected override HttpService _HttpService { get; }
        protected override string _ExpectedUri => "api/weather";

        public HttpServicePostTests()
        {
            _MockHandler
                .When(HttpMethod.Post, $"http://127.0.0.1/{_ExpectedUri}").Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherRequestResponse() { Celsius = 10, Date = "2023-09-22", City = "Budapest" }));

            _HttpClientFactory = new MockHttpClientFactory(_MockHandler, "http://127.0.0.1/");

            _HttpService = new HttpService(_HttpClientFactory, _MemoryCache);
        }

        [Fact]
        public async Task PostAsync_PassRequestObject_SameRequestResponse()
        {
            // Arrange
            var requestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2023-09-22"
            };

            // Act
            var response = await _HttpService.PostAsync<WeatherResponse, WeatherRequest>(requestData);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Budapest", response.City);
            Assert.Equal(10, response.Celsius);
        }

        [Fact]
        public async Task PostAsync_PassRequestObject_DifferentRequestResponse()
        {
            // Arrange
            var requestData = new WeatherRequestResponse()
            {
                City = "Budapest",
                Date = "2023-09-22",
                Celsius = 10
            };

            // Act
            var response = await _HttpService.PostAsync<WeatherRequestResponse>(requestData);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Budapest", response.City);
            Assert.Equal("2023-09-22", response.Date);
            Assert.Equal(10, response.Celsius);
        }
    }
}
