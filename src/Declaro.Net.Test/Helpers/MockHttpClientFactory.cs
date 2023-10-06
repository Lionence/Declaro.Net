using RichardSzalay.MockHttp;

namespace Declaro.Net.Test.Helpers
{
    public class MockHttpClientFactory : IHttpClientFactory
    {
        private readonly MockHttpMessageHandler _mockHttpMessageHandler;
        private readonly Uri? _uri;

        public MockHttpClientFactory(MockHttpMessageHandler mockHttpMessageHandler, string baseAddress)
        {
            _mockHttpMessageHandler = mockHttpMessageHandler ?? throw new ArgumentNullException(nameof(mockHttpMessageHandler));
            _uri = new Uri(baseAddress);
        }

        public HttpClient CreateClient(string name)
        {
            var client = _mockHttpMessageHandler.ToHttpClient();
            if(_uri != null)
            {
                client.BaseAddress = _uri;
            }
            return client;
        }
    }
}
