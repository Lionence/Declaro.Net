using Declaro.Net.Connection;
using Declaro.Net.Test.HttpServiceTests.Base;
using Declaro.Net.Test.TestDataTypes;
using RichardSzalay.MockHttp;
using System.Net;

namespace Declaro.Net.Test.HttpServiceTests
{
    public class HttpServiceDeleteTests : HttpServiceTestBase
    {
        protected override HttpClient _HttpClient { get; }
        protected override HttpService _HttpService { get; }
        protected override string _ExpectedUri => "api/weather";

        public HttpServiceDeleteTests()
        {
            _MockHandler
                .When(HttpMethod.Delete, $"http://127.0.0.1/{_ExpectedUri}").Respond(HttpStatusCode.OK);

            _HttpClient = new HttpClient(_MockHandler);
            _HttpClient.BaseAddress = new Uri("http://127.0.0.1/");

            _HttpService = new HttpService(_HttpClient, _MemoryCache);
        }

        [Fact]
        public async Task DeleteAsync_PassRequestObject()
        {
            // Arrange
            var requestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2023-09-22"
            };

            // Act
            await _HttpService.DeleteAsync(requestData);

            // Assert
        }
    }
}
