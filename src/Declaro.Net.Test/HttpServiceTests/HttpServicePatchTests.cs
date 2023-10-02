using Declaro.Net.Connection;
using Declaro.Net.Test.HttpServiceTests.Base;
using Declaro.Net.Test.TestDataTypes;
using RichardSzalay.MockHttp;
using System.Net.Http.Json;
using System.Net;

namespace Declaro.Net.Test.HttpServiceTests
{
    public class HttpServicePatchTests : HttpServiceTestBase
    {
        protected override HttpClient _HttpClient { get; }
        protected override HttpService _HttpService { get; }
        protected override string _ExpectedUri => "api/weather";

        public HttpServicePatchTests()
        {
            _MockHandler
                .When(HttpMethod.Patch, $"http://127.0.0.1/{_ExpectedUri}").Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherRequestResponse() { Celsius = 10, Date = "2023-09-22", City = "Budapest" }));

            _HttpClient = new HttpClient(_MockHandler);
            _HttpClient.BaseAddress = new Uri("http://127.0.0.1/");

            _HttpService = new HttpService(_HttpClient, _MemoryCache);
        }

        [Fact]
        public async Task PatchAsync_PassRequestObject_SameRequestResponse()
        {
            // Arrange
            var requestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2023-09-22"
            };

            // Act
            var response = await _HttpService.PatchAsync<WeatherResponse, WeatherRequest>(requestData);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Budapest", response.City);
            Assert.Equal(10, response.Celsius);
        }

        [Fact]
        public async Task PatchAsync_PassRequestObject_DifferentRequestResponse()
        {
            // Arrange
            var requestData = new WeatherRequestResponse()
            {
                City = "Budapest",
                Date = "2023-09-22",
                Celsius = 10
            };

            // Act
            var response = await _HttpService.PatchAsync(requestData);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Budapest", response.City);
            Assert.Equal("2023-09-22", response.Date);
            Assert.Equal(10, response.Celsius);
        }
    }
}
