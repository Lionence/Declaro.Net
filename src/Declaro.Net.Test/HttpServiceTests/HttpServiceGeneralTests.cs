using Declaro.Net.Attributes;
using Declaro.Net.Connection;
using Declaro.Net.Exceptions;
using Declaro.Net.Test.TestDataTypes;
using Microsoft.Extensions.Caching.Memory;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;

namespace Declaro.Net.Test.HttpServiceTests
{
    public class HttpServiceGeneralTests
    {
        private readonly Type _HttpServiceType = typeof(HttpService);
        protected string _ExpectedUri_WithQParams => "api/weather?City=Budapest?Date=2023-09-22";
        protected string _ExpectedUri_NoQParams => "api/weather";

        public HttpServiceGeneralTests() { }

        [Fact]
        public void Validate_NonPublicBehavior_WithQParams()
        {
            // Arrange
            var requestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2023-09-22"
            };
            var mock = new MockHttpMessageHandler();
            mock.When($"http://127.0.0.1/{_ExpectedUri_WithQParams}")
                .Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherResponse() { Celsius = 10, City = "Budapest" }));
            var client = new HttpClient(mock);
            var httpService = new HttpService(client, new MemoryCache(new MemoryCacheOptions()));

            // Act
            var weatherAttr = _HttpServiceType.GetMethod("GetHttpConfig", BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(WeatherResponse), typeof(HttpGetAttribute))
                ?.Invoke(obj: null, parameters: null) as HttpGetAttribute;
            Assert.NotNull(weatherAttr);

            var method = _HttpServiceType?.GetMethod("GetQueryParameters", BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(WeatherRequest));
            var queryParams = method?.Invoke(obj: null, parameters: new object[] { requestData, weatherAttr }) as object[];
            Assert.NotNull(queryParams);

            method = _HttpServiceType?.GetMethod("ApplyHttpConfig", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(HttpGetAttribute));
            var args = new object[] { weatherAttr, queryParams, "" };
            method?.Invoke(httpService, args);
            var uri = args[2] as string;

            // Assert
            Assert.Equal(requestData.City, queryParams[0]);
            Assert.Equal(requestData.Date, queryParams[1]);
            Assert.Equal(_ExpectedUri_WithQParams, uri);
        }

        [Fact]
        public void Validate_NonPublicBehavior_NoQParams()
        {
            // Arrange
            var mock = new MockHttpMessageHandler();
            mock.When($"http://127.0.0.1/{_ExpectedUri_WithQParams}")
                .Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherResponse() { Celsius = 10, City = "Budapest" }));
            var client = new HttpClient(mock);
            var httpService = new HttpService(client, new MemoryCache(new MemoryCacheOptions()));

            var weatherAttr = _HttpServiceType.GetMethod("GetHttpConfig", BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(WeatherResponse), typeof(HttpAttribute))
                ?.Invoke(obj: null, parameters: null) as HttpAttribute;
            Assert.NotNull(weatherAttr);

            // Act
            var method = _HttpServiceType?.GetMethod("ApplyHttpConfig", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(HttpAttribute));
            var args = new object?[] { weatherAttr, null, "" };
            method?.Invoke(httpService, args);
            var uri = args[2] as string;

            // Assert
            Assert.Equal(_ExpectedUri_NoQParams, uri);
        }

        [Fact]
        public async Task Validate_UsingQueryParams()
        {
            // Arrange
            var validRequestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2023-09-22"
            };
            var invalidRequestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = null
            };

            var mock = new MockHttpMessageHandler();
            mock.Expect($"http://127.0.0.1/{_ExpectedUri_WithQParams}")
                .Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherResponse() { Celsius = 10, City = "Budapest" }));
            mock.Expect($"http://127.0.0.1/{_ExpectedUri_WithQParams}")
                .Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherResponse() { Celsius = 10, City = "Budapest" }));
            mock.Expect($"http://127.0.0.1/{_ExpectedUri_WithQParams}")
                .Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherRequestResponse() { Celsius = 10, City = validRequestData.City, Date = validRequestData.Date }));
            var client = new HttpClient(mock);
            client.BaseAddress = new Uri("http://127.0.0.1");
            var httpService = new HttpService(client, new MemoryCache(new MemoryCacheOptions()));

            // Act
            // Assert
            await Assert.ThrowsAsync<FormatException>(async () => await httpService.GetAsync<WeatherResponse>(
                queryParameters: null));
            await Assert.ThrowsAsync<FormatException>(async () => await httpService.GetAsync<WeatherResponse>(
                queryParameters: new object[0]));
            await Assert.ThrowsAsync<FormatException>(async () => await httpService.GetAsync<WeatherResponse>(
                queryParameters: new object[] { validRequestData.City }));
            Assert.NotNull(await httpService.GetAsync<WeatherResponse>(
                queryParameters: new object[] { validRequestData.City, validRequestData.Date }));
            await Assert.ThrowsAsync<FormatException>(async () => await httpService.GetAsync<WeatherResponse>(
                queryParameters: new object[] { validRequestData.City, validRequestData.City, "extra-invalid-query-parameter" }));
            Assert.NotNull(await httpService.GetAsync<WeatherResponse, WeatherRequest>(validRequestData));
            Assert.NotNull(await httpService.GetAsync(
                new WeatherRequestResponse()
                {
                    Celsius = 10,
                    City = validRequestData.City,
                    Date = validRequestData.Date,
                }));
        }

        [Fact]
        public async Task Validate_InternalServerErrorResponse()
        {
            // Arrange
            var requestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2023-09-22"
            };

            var mock = new MockHttpMessageHandler();
            mock.When($"http://127.0.0.1/{_ExpectedUri_WithQParams}")
                .Respond(HttpStatusCode.InternalServerError);
            var client = new HttpClient(mock);
            client.BaseAddress = new Uri("http://127.0.0.1");
            var httpService = new HttpService(client, new MemoryCache(new MemoryCacheOptions()));

            // Act
            // Assert
            var exception = await Assert.ThrowsAsync<HttpClientException>(async () => await httpService.GetAsync<WeatherResponse>(
                queryParameters: new object[] { requestData.City, requestData.Date }));
            Assert.True(exception.StatusCode == 500);
            Assert.True(exception.Message == HttpStatusCode.InternalServerError.ToString());
            
        }

        [Fact]
        public async Task Validate_NotFoundResponse()
        {
            // Arrange
            var requestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2023-09-22"
            };

            var mock = new MockHttpMessageHandler();
            mock.When($"http://127.0.0.1/{_ExpectedUri_WithQParams}")
                .Respond(HttpStatusCode.NotFound);
            var client = new HttpClient(mock);
            client.BaseAddress = new Uri("http://127.0.0.1");
            var httpService = new HttpService(client, new MemoryCache(new MemoryCacheOptions()));

            // Act
            // Assert
            var exception = await Assert.ThrowsAsync<HttpClientException>(async () => await httpService.GetAsync<WeatherResponse>(
                queryParameters: new object[] { requestData.City, requestData.City }));
            Assert.True(exception.StatusCode == 404);
            Assert.True(exception.Message == HttpStatusCode.NotFound.ToString());
            
        }
    }
}
