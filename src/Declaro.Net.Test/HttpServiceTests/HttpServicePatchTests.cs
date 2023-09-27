using Declaro.Net.Connection;
using Declaro.Net.Test.HttpServiceTests.Base;
using Declaro.Net.Test.TestDataTypes;
using RichardSzalay.MockHttp;
using System.Net.Http.Json;
using System.Net;
using Declaro.Net.Attributes;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Declaro.Net.Test.HttpServiceTests
{
    public class HttpServicePatchTests : HttpServiceTestBase
    {
        private HttpClient _httpClient;
        private string _expectedUri = "api/weather";

        protected override HttpService _HttpService { get; }

        public HttpServicePatchTests()
        {
            _MockHandler
                .When(HttpMethod.Patch, $"http://127.0.0.1/{_expectedUri}").Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherRequestResponse() { Celsius = 10, Date = "2023-09-22", City = "Budapest" }));

            _httpClient = new HttpClient(_MockHandler);
            _httpClient.BaseAddress = new Uri("http://127.0.0.1/");

            _HttpService = new HttpService(_httpClient, _MemoryCache);
        }

        [Fact]
        public async Task ValidatePatch_PassRequestObject()
        {
            // Arrange
            var requestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2023-09-22"
            };

            var weatherAttr = _HttpServiceType.GetMethod("GetHttpConfig", BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(WeatherResponse), typeof(HttpPatchAttribute))
                ?.Invoke(obj: null, parameters: null) as HttpPatchAttribute;

            // Act
            var method = _HttpServiceType?.GetMethod("ApplyHttpConfig", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(HttpAttribute));
            var args = new object[] { weatherAttr, null, "" };
            method?.Invoke(_HttpService, args);
            var uri = args[2] as string;

            // Assert
            Assert.Equal(_expectedUri, uri);
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
            var response = await _HttpService.PatchAsync<WeatherRequestResponse>(requestData);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Budapest", response.City);
            Assert.Equal("2023-09-22", response.Date);
            Assert.Equal(10, response.Celsius);
        }
    }
}
