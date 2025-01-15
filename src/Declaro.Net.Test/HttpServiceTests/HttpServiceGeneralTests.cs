using Declaro.Net.Attributes;
using Declaro.Net.Connection;
using Declaro.Net.Exceptions;
using Declaro.Net.Test.Helpers;
using Declaro.Net.Test.TestDataTypes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;

namespace Declaro.Net.Test.HttpServiceTests
{
    public class HttpServiceGeneralTests
    {
        private readonly Type _HttpServiceType = typeof(HttpService);
        protected string _ExpectedUri_NoRequestArguments => "api/weather";
        protected string _ExpectedUri_NoRequestArguments_WithQueryParameters => "api/weather?District=13";
        protected string _ExpectedUri_WithRequestArguments => "api/weather?City=Budapest&Date=2023-09-22";
        protected string _ExpectedUri_WithRequestArguments_WithQueryParameters => "api/weather?City=Budapest&Date=2023-09-22&District=13";
        
        public HttpServiceGeneralTests() { }

        [Fact]
        public void Validate_Uri_WithRequestArgument_NoQueryParameters()
        {
            // Arrange
            var requestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2023-09-22"
            };
            var mock = new MockHttpMessageHandler();
            mock.When($"http://127.0.0.1/{_ExpectedUri_WithRequestArguments}")
                .Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherResponse() { Celsius = 10, City = "Budapest" }));
            var factory = new MockHttpClientFactory(mock, "http://127.0.0.1/");
            var logger = new Logger<HttpService>(LoggerFactory.Create(configure => { }));
            var httpService = new HttpService(logger, factory, new MemoryCache(new MemoryCacheOptions()));

            // Act
            var weatherAttr = _HttpServiceType.GetMethod("GetHttpConfig", BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(WeatherResponse), typeof(HttpGetAttribute))
                ?.Invoke(obj: null, parameters: null) as HttpGetAttribute;
            Assert.NotNull(weatherAttr);

            var method = _HttpServiceType?.GetMethod("GetRequestArguments", BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(WeatherRequest));
            var requestArguments = method?.Invoke(obj: null, parameters: new object[] { requestData, weatherAttr }) as object[];
            Assert.NotNull(requestArguments);

            method = _HttpServiceType?.GetMethod("GetUri", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(HttpGetAttribute));
            var args = new object[] { weatherAttr, requestArguments, null };
            var uri = method?.Invoke(httpService, args) as string;

            // Assert
            Assert.Equal(requestData.City, requestArguments[0]);
            Assert.Equal(requestData.Date, requestArguments[1]);
            Assert.Equal(_ExpectedUri_WithRequestArguments, uri);
        }

        [Fact]
        public void Validate_Uri_NoRequestArguments_NoQueryParameters()
        {
            // Arrange
            var mock = new MockHttpMessageHandler();
            mock.When($"http://127.0.0.1/{_ExpectedUri_WithRequestArguments}")
                .Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherResponse() { Celsius = 10, City = "Budapest" }));
            var factory = new MockHttpClientFactory(mock, "http://127.0.0.1/");
            var logger = new Logger<HttpService>(LoggerFactory.Create(configure => { }));
            var httpService = new HttpService(logger, factory, new MemoryCache(new MemoryCacheOptions()));

            var weatherAttr = _HttpServiceType.GetMethod("GetHttpConfig", BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(WeatherResponse), typeof(HttpAttribute))
                ?.Invoke(obj: null, parameters: null) as HttpAttribute;
            Assert.NotNull(weatherAttr);

            // Act
            var method = _HttpServiceType?.GetMethod("GetUri", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(HttpAttribute));
            var args = new object?[] { weatherAttr, null, null };
            var uri = method?.Invoke(httpService, args) as string;

            // Assert
            Assert.Equal(_ExpectedUri_NoRequestArguments, uri);
        }

        [Fact]
        public void Validate_Uri_WithRequestArguments_WithQueryParameters()
        {
            // Arrange
            var requestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2023-09-22"
            };
            var queryParameters = new (string, string)[] { ("District", "13") };
            var mock = new MockHttpMessageHandler();
            mock.When($"http://127.0.0.1/{_ExpectedUri_WithRequestArguments}")
                .Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherResponse() { Celsius = 10, City = "Budapest" }));
            var factory = new MockHttpClientFactory(mock, "http://127.0.0.1/");
            var logger = new Logger<HttpService>(LoggerFactory.Create(configure => { }));
            var httpService = new HttpService(logger, factory, new MemoryCache(new MemoryCacheOptions()));

            // Act
            var weatherAttr = _HttpServiceType.GetMethod("GetHttpConfig", BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(WeatherResponse), typeof(HttpGetAttribute))
                ?.Invoke(obj: null, parameters: null) as HttpGetAttribute;
            Assert.NotNull(weatherAttr);

            var method = _HttpServiceType?.GetMethod("GetRequestArguments", BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(WeatherRequest));
            var requestArguments = method?.Invoke(obj: null, parameters: new object[] { requestData, weatherAttr }) as object[];
            Assert.NotNull(requestArguments);

            method = _HttpServiceType?.GetMethod("GetUri", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(HttpGetAttribute));
            var args = new object[] { weatherAttr, requestArguments, queryParameters };
            var uri = method?.Invoke(httpService, args) as string;

            // Assert
            Assert.Equal(requestData.City, requestArguments[0]);
            Assert.Equal(requestData.Date, requestArguments[1]);
            Assert.Equal(_ExpectedUri_WithRequestArguments_WithQueryParameters, uri);
        }

        [Fact]
        public void Validate_Uri_NoRequestArguments_WithQueryParameters()
        {
            // Arrange
            var queryParameters = new (string, string)[] { ("District", "13") };
            var mock = new MockHttpMessageHandler();
            mock.When($"http://127.0.0.1/{_ExpectedUri_WithRequestArguments}")
                .Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherResponse() { Celsius = 10, City = "Budapest" }));
            var factory = new MockHttpClientFactory(mock, "http://127.0.0.1/");
            var logger = new Logger<HttpService>(LoggerFactory.Create(configure => { }));
            var httpService = new HttpService(logger, factory, new MemoryCache(new MemoryCacheOptions()));

            var weatherAttr = _HttpServiceType.GetMethod("GetHttpConfig", BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(WeatherResponse), typeof(HttpAttribute))
                ?.Invoke(obj: null, parameters: null) as HttpAttribute;
            Assert.NotNull(weatherAttr);

            // Act
            var method = _HttpServiceType?.GetMethod("GetUri", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.MakeGenericMethod(typeof(HttpAttribute));
            var args = new object?[] { weatherAttr, null, queryParameters };
            var uri = method?.Invoke(httpService, args) as string;

            // Assert
            Assert.Equal(_ExpectedUri_NoRequestArguments_WithQueryParameters, uri);
        }

        [Fact]
        public async Task Validate_UsingRequestArguments()
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
            mock.Expect($"http://127.0.0.1/{_ExpectedUri_WithRequestArguments}")
                .Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherResponse() { Celsius = 10, City = "Budapest" }));
            mock.Expect($"http://127.0.0.1/{_ExpectedUri_WithRequestArguments}")
                .Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherResponse() { Celsius = 10, City = "Budapest" }));
            mock.Expect($"http://127.0.0.1/{_ExpectedUri_WithRequestArguments}")
                .Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherRequestResponse() { Celsius = 10, City = validRequestData.City, Date = validRequestData.Date }));
            var factory = new MockHttpClientFactory(mock, "http://127.0.0.1/");
            var logger = new Logger<HttpService>(LoggerFactory.Create(configure => { }));
            var httpService = new HttpService(logger, factory, new MemoryCache(new MemoryCacheOptions()));

            // Act
            // Assert
            await Assert.ThrowsAsync<FormatException>(async () => await httpService.GetAsync<WeatherResponse>(
                requestArguments: null));
            await Assert.ThrowsAsync<FormatException>(async () => await httpService.GetAsync<WeatherResponse>(
                requestArguments: new object[0]));
            await Assert.ThrowsAsync<FormatException>(async () => await httpService.GetAsync<WeatherResponse>(
                requestArguments: new object[] { validRequestData.City }));
            Assert.NotNull(await httpService.GetAsync<WeatherResponse>(
                requestArguments: new object[] { validRequestData.City, validRequestData.Date }));
            await Assert.ThrowsAsync<FormatException>(async () => await httpService.GetAsync<WeatherResponse>(
                requestArguments: new object[] { validRequestData.City, validRequestData.City, "an-extra-invalid-request-argument-added" }));
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
            mock.When($"http://127.0.0.1/{_ExpectedUri_WithRequestArguments}")
                .Respond(HttpStatusCode.InternalServerError);
            var client = new HttpClient(mock);
            client.BaseAddress = new Uri("http://127.0.0.1");
            var factory = new MockHttpClientFactory(mock, "http://127.0.0.1/");
            var logger = new Logger<HttpService>(LoggerFactory.Create(configure => { }));
            var httpService = new HttpService(logger, factory, new MemoryCache(new MemoryCacheOptions()));

            // Act
            // Assert
            var exception = await Assert.ThrowsAsync<HttpClientException>(async () => await httpService.GetAsync<WeatherResponse>(
                requestArguments: new object[] { requestData.City, requestData.Date }));
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
            mock.When($"http://127.0.0.1/{_ExpectedUri_WithRequestArguments}")
                .Respond(HttpStatusCode.NotFound);
            var factory = new MockHttpClientFactory(mock, "http://127.0.0.1/");
            var logger = new Logger<HttpService>(LoggerFactory.Create(configure => { }));
            var httpService = new HttpService(logger, factory, new MemoryCache(new MemoryCacheOptions()));

            // Act
            // Assert
            var exception = await Assert.ThrowsAsync<HttpClientException>(async () => await httpService.GetAsync<WeatherResponse>(
                requestArguments: new object[] { requestData.City, requestData.City }));
            Assert.True(exception.StatusCode == 404);
            Assert.True(exception.Message == HttpStatusCode.NotFound.ToString());
        }

        [Fact]
        public async Task Validate_DependencyInjection_Without_Action()
        {
            // Arrange
            var expectedUri = "api/weather?City=Budapest&Date=2023-09-22&District=13";
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            mockHttpMessageHandler
                .Expect(HttpMethod.Get, $"{expectedUri}").Respond(HttpStatusCode.OK,
                    JsonContent.Create(new CachedWeatherResponse() { Celsius = 10, City = "Budapest" }));

            var services = new ServiceCollection();
            services.AddHttpService();

            // Act
            var factory = services.Single(s => s.IsKeyedService && s.ServiceKey == "f7b68ed9-749f-4d9f-a537-4416e6084b30_Declaro_HttpClientFactory");
            var client = typeof(HttpClientFactory).GetMethod("CreateClient")?.Invoke(factory.KeyedImplementationInstance, ["test"]) as HttpClient;

            // Assert
            Assert.True(client != null && client.BaseAddress == null);

            var mockLogger = new Mock<ILogger<HttpService>>();
            var httpService = new HttpService(mockLogger.Object, factory.KeyedImplementationInstance as IHttpClientFactory, null);

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await httpService.GetAsync<CachedWeatherResponse, WeatherRequest>(
                new WeatherRequest()
                {
                    City = "Budapest",
                    Date = "2023-09-22"
                },
                queryParameters: ("District", "13")));

        }

        [Fact]
        public async Task Validate_DependencyInjection_With_Action()
        {
            // Arrange
            var expectedBaseAddress = "http://127.0.0.1/";
            var expectedUri = "api/weather?City=Budapest&Date=2023-09-22&District=13";
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            mockHttpMessageHandler
                .Expect(HttpMethod.Get, $"{expectedBaseAddress}{expectedUri}").Respond(HttpStatusCode.OK,
                    JsonContent.Create(new CachedWeatherResponse() { Celsius = 10, City = "Budapest" }));

            var services = new ServiceCollection();
            services.AddHttpService(client =>
            {
                client = mockHttpMessageHandler.ToHttpClient();
                client.BaseAddress = new Uri(expectedBaseAddress);
                return client;
            });

            // Act
            var factory = services.Single(s => s.IsKeyedService && s.ServiceKey == "f7b68ed9-749f-4d9f-a537-4416e6084b30_Declaro_HttpClientFactory");
            var client = typeof(HttpClientFactory).GetMethod("CreateClient")?.Invoke(factory.KeyedImplementationInstance, ["test"]) as HttpClient;

            // Assert
            Assert.True(client?.BaseAddress?.ToString() == expectedBaseAddress);

            // Arrange 2
            var mockLogger = new Mock<ILogger<HttpService>>();
            var httpService = new HttpService(mockLogger.Object, factory.KeyedImplementationInstance as IHttpClientFactory, null);

            // Act 2
            var response = await httpService.GetAsync<CachedWeatherResponse, WeatherRequest>(
                new WeatherRequest()
                {
                    City = "Budapest",
                    Date = "2023-09-22"
                },
                queryParameters: ("District", "13"));

            // Assert 2
            Assert.NotNull(response);
            Assert.Equal("Budapest", response.City);
            Assert.Equal(10, response.Celsius);
        }

        [Fact]
        public void Validate_Missing_MemoryCache_Logs_Warning()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<HttpService>>();
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            var mockHttpClientFactory = new MockHttpClientFactory(mockHttpMessageHandler, "http://127.0.0.1/");
            var httpService = new HttpService(mockLogger.Object, mockHttpClientFactory, null);

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task Validate_Without_RequestArgumentAttribute()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<HttpService>>();
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            var mockHttpClientFactory = new MockHttpClientFactory(mockHttpMessageHandler, "http://127.0.0.1/");
            var httpService = new HttpService(mockLogger.Object, mockHttpClientFactory, null);
            var expectedUri = "api/cities/asd";

            mockHttpMessageHandler
                .Expect(HttpMethod.Get, $"http://127.0.0.1/{expectedUri}").Respond(HttpStatusCode.OK,
                    JsonContent.Create(new CityDataWithoutRequestParameterArgument()
                    {
                        Name = "Budapest",
                        Country = "Hungary"
                    }));

            // Act
            var response = await httpService.GetAsync<CityDataWithoutRequestParameterArgument>(requestArguments: ["asd"]);

            // Assert

            Assert.True(response.Name == "Budapest");
            Assert.True(response.Country == "Hungary");
        }

        [Fact]
        public async Task Validate_JsonIgnore_Attribute()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<HttpService>>();
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            var mockHttpClientFactory = new MockHttpClientFactory(mockHttpMessageHandler, "http://127.0.0.1/");
            var httpService = new HttpService(mockLogger.Object, mockHttpClientFactory, null);
            var expectedUri = "api/cities/asd";

            mockHttpMessageHandler
                .Expect(HttpMethod.Get, $"http://127.0.0.1/{expectedUri}").Respond(HttpStatusCode.OK,
                    JsonContent.Create(new JsonIgnoreCityData()
                    {
                        Id = "asd",
                        Name = "Budapest",
                        Country = "Hungary"
                    }));

            // Act

            var response = await httpService.GetAsync<JsonIgnoreCityData>(requestArguments: ["asd"]);

            // Assert

            Assert.True(string.IsNullOrEmpty(response.Id));
            Assert.True(response.Name == "Budapest");
            Assert.True(response.Country == "Hungary");
        }

        [Fact]
        public async Task Validate_FromJsonProperty_Feature()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<HttpService>>();
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            var mockHttpClientFactory = new MockHttpClientFactory(mockHttpMessageHandler, "http://127.0.0.1/");
            var httpService = new HttpService(mockLogger.Object, mockHttpClientFactory, null);
            var expectedUri = "api/weather?City=Budapest";

            mockHttpMessageHandler
                .Expect(HttpMethod.Get, $"http://127.0.0.1/{expectedUri}").Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherWithCityClass()
                    {
                        Celsius = 10,
                        City = "Budapest",
                        CityData = new CityData() { City = "Budapest", Country = "Hungary" }
                    }));

            // Act

            var response = await httpService.GetAsync<CityData>(requestArguments: ["Budapest"]);

            // Assert

            Assert.True(response.City == "Budapest");
            Assert.True(response.Country == "Hungary");
        }
    }
}