using Declaro.Net.Attributes;
using Declaro.Net.Connection;
using Declaro.Net.Test.HttpServiceTests.Base;
using Declaro.Net.Test.TestDataTypes;
using RichardSzalay.MockHttp;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;

namespace Declaro.Net.Test.HttpServiceTests
{
    public class HttpServiceGetTests : HttpServiceTestBase
    {
        private HttpClient _httpClient;
        private string _expectedUri = "api/weather?City=Budapest?Date=2023-09-22";

        protected override HttpService _HttpService { get; }

        public HttpServiceGetTests()
        {
            _MockHandler
                .When(HttpMethod.Get, $"http://127.0.0.1/{_expectedUri}").Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherResponse() { Celsius = 10, City = "Budapest" }));

            _httpClient = new HttpClient(_MockHandler);
            _httpClient.BaseAddress = new Uri("http://127.0.0.1/");

            _HttpService = new HttpService(_httpClient, _MemoryCache);
        }

        [Fact]
        public void ValidateGet_PassRequestObject()
        {
            // Arrange
            var requestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2023-09-22"
            };

            var weatherAttr = _HttpServiceType.GetMethod("GetHttpConfig", BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(WeatherResponse), typeof(HttpGetAttribute))
                ?.Invoke(obj: null, parameters: null) as HttpGetAttribute;

            // Act
            var method = _HttpServiceType?.GetMethod("GetQueryParameters", BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(WeatherRequest));
            var queryParams = method?.Invoke(obj: null, parameters: new object[] { requestData, weatherAttr }) as object[];
            Assert.NotNull(queryParams);

            method = _HttpServiceType?.GetMethod("ApplyHttpConfig", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(HttpAttribute));
            var args = new object[] { weatherAttr, queryParams, "" };
            method?.Invoke(_HttpService, args);
            var uri = args[2] as string;

            // Assert
            Assert.Equal(requestData.City, queryParams[0]);
            Assert.Equal(requestData.Date, queryParams[1]);
            Assert.Equal(_expectedUri, uri);
        }

        [Fact]
        public async Task GetAsync_PassRequestObject()
        {
            // Arrange
            var requestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2023-09-22"
            };

            // Act
            var response = await _HttpService.GetAsync<WeatherResponse, WeatherRequest>(requestData);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Budapest", response.City);
            Assert.Equal(10, response.Celsius);
        }

        [Fact]
        public async Task GetAsync_PassQueryParameters()
        {
            // Arrange

            // Act
            var response = await _HttpService.GetAsync<WeatherResponse>(queryParameters: new object[] { "Budapest", "2023-09-22" });

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Budapest", response.City);
            Assert.Equal(10, response.Celsius);
        }

        [Fact]
        public async Task Validate_GetAsyncWithCaching()
        {
            // Arrange
            var requestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2023-09-22"
            };

            var allAttr = _HttpServiceType.GetField("_httpConfigCache", BindingFlags.Static | BindingFlags.NonPublic)
                ?.GetValue(null) as ReadOnlyDictionary<Type, HttpAttribute[]>;
            Assert.NotNull(allAttr);
            var weatherAttr = allAttr[typeof(WeatherResponse)].Single(a => a.GetType() == typeof(HttpGetAttribute));

            var method = _HttpServiceType?.GetMethod("GetQueryParameters", BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(WeatherRequest));
            var queryParams = method?.Invoke(obj: null, parameters: new object[] { requestData, weatherAttr }) as object[];
            Assert.NotNull(queryParams);

            var cacheKey = _HttpServiceType?.GetMethod("GetCacheKey", BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(WeatherResponse), typeof(HttpGetAttribute))
                ?.Invoke(obj: null, parameters: new object[] { weatherAttr, queryParams });
            Assert.NotNull(cacheKey);

            Thread.Sleep(3000);

            var cacheStillExist = _MemoryCache.TryGetValue(cacheKey, out _);

            // Act
            var cachedBeforeCall = _MemoryCache.TryGetValue(cacheKey, out _);
            var response1 = await _HttpService.GetAsync<WeatherCached>(queryParameters: new object[] { requestData.City, requestData.Date });

            var cachedAfterCall = _MemoryCache.TryGetValue(cacheKey, out _);
            var response2 = await _HttpService.GetAsync<WeatherCached>(queryParameters: new object[] { requestData.City, requestData.Date });

            // Assert
            Assert.False(cachedBeforeCall);
            Assert.NotNull(response1);
            Assert.Equal("Budapest", response1.City);
            Assert.Equal(10, response1.Celsius);

            Assert.True(cachedAfterCall);
            Assert.NotNull(response2);
            Assert.Equal("Budapest", response2.City);
            Assert.Equal(10, response2.Celsius);

            Assert.False(cacheStillExist);
        }
    }
}