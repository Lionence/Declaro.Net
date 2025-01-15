namespace Declaro.Net.Connection;

public class HttpClientFactory : IHttpClientFactory
{
    private readonly Func<HttpClient, HttpClient>? _configureHttpClient;

    public HttpClientFactory(Func<HttpClient, HttpClient>? configureHttpClient = null)
    {
        _configureHttpClient = configureHttpClient;
    }

    public HttpClient CreateClient(string name)
    {
        var client = new HttpClient();
        if(_configureHttpClient != null)
        {
            client = _configureHttpClient(client);
        }
        return client;
    }
}
