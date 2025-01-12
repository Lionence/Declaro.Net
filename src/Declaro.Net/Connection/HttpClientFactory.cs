namespace Declaro.Net.Connection;

public class HttpClientFactory : IHttpClientFactory
{
    private readonly Action<HttpClient>? _configureHttpClient;

    public HttpClientFactory(Action<HttpClient>? configureHttpClient = null)
    {
        _configureHttpClient = configureHttpClient;
    }

    public HttpClient CreateClient(string name)
    {
        var client = new HttpClient();
        if(_configureHttpClient != null)
        {
            _configureHttpClient(client);
        }
        return client;
    }
}
