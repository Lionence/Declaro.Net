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
        protected override HttpService _HttpService { get; }
        protected override HttpClient _HttpClient { get; }
        protected override string _ExpectedUri => "api/weather?City=Budapest?Date=2023-09-22";

        public HttpServiceGetTests()
        {
            _MockHandler
                .When(HttpMethod.Get, $"http://127.0.0.1/{_ExpectedUri}").Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherResponse() { Celsius = 10, City = "Budapest" }));

            _HttpClient = new HttpClient(_MockHandler);
            _HttpClient.BaseAddress = new Uri("http://127.0.0.1/");

            _HttpService = new HttpService(_HttpClient, _MemoryCache);
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