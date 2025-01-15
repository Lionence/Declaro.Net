using Declaro.Net.Attributes;
using System.Reflection;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Declaro.Net.Exceptions;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Declaro.Net.Connection;

/// <summary>
/// Service that handles HTTP requests for Declaro.NET based on implicit definitions.
/// </summary>
public sealed class HttpService
{
    private readonly ILogger<HttpService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache? _memoryCache;
    private static readonly ReadOnlyDictionary<Type, HttpAttribute[]> _httpConfigCache;
    private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    // Builds _HttpConfigCache from all types of the assembly, that have any of the HttpAttributes defined.
    static HttpService()
    {
        var httpConfigCache = new Dictionary<Type, HttpAttribute[]>();

        AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
            .Where(type => type.GetCustomAttributes<HttpAttribute>(true).Any()).ToList()
            .ForEach(type =>
            {
                IEnumerable<HttpAttribute> attributes = type.GetCustomAttributes<HttpAttribute>(true);
                foreach (HttpAttribute attribute in attributes)
                {
                    if (attribute.ResponseType == null )
                    {
                        attribute.ResponseType = type;
                    }

                    if (attribute.RequestType == null )
                    {
                        attribute.RequestType = type;
                    }

                    var requestArgs = attribute.RequestType?.GetProperties()
                        .Where(p => Attribute.IsDefined(p, typeof(RequestArgumentAttribute)))
                        .Select(p => new { p.GetCustomAttribute<RequestArgumentAttribute>()?.Index, Property = p })
                        .OrderBy(g => g.Index);
                    attribute.ArgumentProperties = requestArgs?.Select(g => g.Property).ToArray();
                }

                var groupedAttributes = attributes.GroupBy(a => a.ResponseType)
                    .Select(g => new { g.First().ResponseType, Attributes = g.ToArray()});
                foreach (var group in groupedAttributes)
                {
                    httpConfigCache.Add(group.ResponseType ?? throw new NullReferenceException(nameof(group.ResponseType)), group.Attributes);
                }
            });

        _httpConfigCache = new ReadOnlyDictionary<Type, HttpAttribute[]>(httpConfigCache);
    }

    public HttpService(ILogger<HttpService> logger, [FromKeyedServices(Constants.HTTPCLIENTFACTORY_DI_KEY)] IHttpClientFactory httpClientFactory, IMemoryCache? memoryCache)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(HttpClient));
        _memoryCache = memoryCache;

        bool warn = false;
        foreach (var config in _httpConfigCache)
        {
            var hasCachedAttribute = config.Value.Any(a => a.GetType().GetInterface(nameof(ICacheAttribute)) != null);
            if (memoryCache == null && hasCachedAttribute)
            {
                warn = true;
                break;
            }
        }
        if (warn)
        {
            logger.LogWarning("{HttpService} detected cached configuration but is not able to cache because {memoryCache} is missing!", nameof(HttpService), nameof(memoryCache));
        }
    }

    /// <summary>
    /// Sends HTTP GET request.
    /// </summary>
    /// <typeparam name="TResponse">Data type to recieve.</typeparam>
    /// <typeparam name="TRequest">Data type to send.</typeparam>
    /// <param name="data">Data object to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Resource requested for specified type.</returns>
    public async ValueTask<TResponse> GetAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default, params (string, string)[]? queryParameters)
        where TResponse : class
    {
        var config = GetHttpConfig<TResponse, HttpGetAttribute>();
        var arguments = GetRequestArguments(data, config);
        return await GetAsync<TResponse>(arguments, ct, queryParameters);
    }

    /// <summary>
    /// Sends HTTP GET request.
    /// </summary>
    /// <typeparam name="TData">Type of both request and response data.</typeparam>
    /// <param name="requestData">The request data to send.</param>
    /// <param name="ct">Cancellation token.</param>
    public async ValueTask<TData> GetAsync<TData>(TData requestData, CancellationToken ct = default, params (string, string)[]? queryParameters)
        where TData : class
        => await GetAsync<TData, TData>(requestData, ct, queryParameters);

    /// <summary>
    /// Sends HTTP GET request.
    /// </summary>
    /// <typeparam name="TResponse">Data type to recieve.</typeparam>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="requestArguments">Request arguments.</param>
    /// <returns>Resource requested for specified type.</returns>
    public async ValueTask<TResponse> GetAsync<TResponse>(object[]? requestArguments, CancellationToken ct = default, params (string, string)[]? queryParameters)
        where TResponse : class
    {
        var config = GetHttpConfig<TResponse, HttpGetAttribute>();

        int requiredArgumentsCount = GetRequiredArguments(config.ApiEndpoint);
        int? argLength = config.ArgumentProperties?.Length;
        if (requiredArgumentsCount != requestArguments?.Length && argLength.HasValue && argLength.Value != requestArguments?.Length)
        {
            throw new FormatException($"Number of required request arguments '{argLength.Value}' does not equal to actual number of request arguments '{requestArguments?.Length}'!");
        }

        var uri = GetUri(config, requestArguments, queryParameters);

        if (_memoryCache != null && _memoryCache.TryGetValue(uri, out ICacheEntry? cacheEntry))
        {
            return cacheEntry?.Value as TResponse ?? throw new UnreachableException();
        }

        var client = CreateHttpClient(config, uri);
        var responseMessage = await client.GetAsync(uri, ct);
        var response = await DeserializeResponse<TResponse>(responseMessage, config.FromJsonProperty);

        if (response == null)
        {
            throw new NullReferenceException($"Empty response for GET: {typeof(TResponse).Name} on endpoint '{config?.ApiEndpoint}'");
        }

        TimeSpan.TryParse(config?.CacheTime, out var cachingTime);
        if (_memoryCache != null && cachingTime > TimeSpan.Zero)
        {
            cacheEntry = _memoryCache.CreateEntry(uri);
            cacheEntry.Value = response;
            _memoryCache.Set(uri, cacheEntry, cachingTime);
        }

        return response;
    }

    /// <summary>
    /// GET query handling, returning a collection of data.
    /// </summary>
    /// <typeparam name="TData">Data type to both recieve and send.</typeparam>
    /// <param name="data">Object to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>With data of desired type - TResponse.</returns>
    public async ValueTask<ICollection<TData>?> ListAsync<TData>(TData data, CancellationToken ct = default, params (string, string)[]? queryParameters)
        where TData : class
        => await ListAsync<TData, TData>(data, ct, queryParameters);

    /// <summary>
    /// GET query handling, returning a collection of data.
    /// </summary>
    /// <typeparam name="TResponse">Data type to recieve.</typeparam>
    /// <typeparam name="TRequest">Data type to send.</typeparam>
    /// <param name="data">Object to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>With data of desired type - TResponse.</returns>
    public async ValueTask<ICollection<TResponse>?> ListAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default, params (string, string)[]? queryParameters)
        where TRequest : notnull
        where TResponse : class
    {
        var config = GetHttpConfig<TResponse, HttpListAttribute>();
        var uri = GetUri(config, null, queryParameters);

        if (_memoryCache != null && _memoryCache.TryGetValue(uri, out ICacheEntry? cacheEntry))
        {
            return cacheEntry?.Value as ICollection<TResponse> ?? throw new UnreachableException();
        }

        var client = CreateHttpClient(config, uri);
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        var response = await DeserializeResponse<ICollection<TResponse>>(await client.GetAsync(uri, ct), config.FromJsonProperty);

        TimeSpan.TryParse(config?.CacheTime, out var cachingTime);
        if (_memoryCache != null && cachingTime > TimeSpan.Zero)
        {
            cacheEntry = _memoryCache.CreateEntry(uri);
            cacheEntry.Value = response;
            _memoryCache.Set(uri, cacheEntry, cachingTime);
        }

        return response;
    }

    /// <summary>
    /// Sends HTTP POST request. 
    /// </summary>
    /// <typeparam name="TData">Data type to both recieve and send.</typeparam>
    /// <param name="data">Object to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>With data of desired type - TResponse.</returns>
    public async ValueTask<TData?> PostAsync<TData>(TData data, CancellationToken ct = default, params (string, string)[]? queryParameters)
        where TData : notnull
        => await PostAsync<TData, TData>(data, ct, queryParameters);

    /// <summary>
    /// Sends HTTP POST request. 
    /// </summary>
    /// <typeparam name="TResponse">Data type to recieve.</typeparam>
    /// <typeparam name="TRequest">Data type to send.</typeparam>
    /// <param name="data">Object to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>With data of desired type - TResponse.</returns>
    public async ValueTask<TResponse?> PostAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default, params (string, string)[]? queryParameters)
        where TRequest : notnull
    {
        var config = GetHttpConfig<TResponse, HttpPostAttribute>();
        var uri = GetUri(config, null, queryParameters);
        var client = CreateHttpClient(config, uri);
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        var response = await DeserializeResponse<TResponse>(await client.PostAsync(uri, content, ct), config.FromJsonProperty);
        return response;
    }

    /// <summary>
    /// Sends HTTP PUT request.
    /// </summary>
    /// <typeparam name="TData">Data type to both recieve and send.</typeparam>
    /// <param name="data">Object to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>With data of desired type - TResponse.</returns>
    public async ValueTask<TData?> PutAsync<TData>(TData data, CancellationToken ct = default, params (string, string)[]? queryParameters)
        where TData : notnull
        => await PutAsync<TData, TData>(data, ct, queryParameters);

    /// <summary>
    /// Sends HTTP PUT request. 
    /// </summary>
    /// <typeparam name="TResponse">Data type to recieve.</typeparam>
    /// <typeparam name="TRequest">Data type to send.</typeparam>
    /// <param name="data">Object to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>With data of desired type - TResponse.</returns>
    public async ValueTask<TResponse?> PutAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default, params (string, string)[]? queryParameters)
        where TRequest : notnull
    {
        var config = GetHttpConfig<TResponse, HttpPutAttribute>();
        var uri = GetUri(config, null, queryParameters);
        var client = CreateHttpClient(config, uri);
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        var response = await DeserializeResponse<TResponse>(await client.PutAsync(uri, content, ct), config.FromJsonProperty);
        return response;
    }

    /// <summary>
    /// Sends HTTP PATCH request.
    /// </summary>
    /// <typeparam name="TData">Data type to both recieve and send.</typeparam>
    /// <param name="data">Object to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>With data of desired type - TResponse.</returns>
    public async ValueTask<TData?> PatchAsync<TData>(TData data, CancellationToken ct = default, params (string, string)[]? queryParameters)
        where TData : notnull
        => await PatchAsync<TData, TData>(data, ct, queryParameters);

    /// <summary>
    /// Sends HTTP PATCH request. 
    /// </summary>
    /// <typeparam name="TResponse">Data type to recieve.</typeparam>
    /// <typeparam name="TRequest">Data type to send.</typeparam>
    /// <param name="data">Object to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>With data of desired type - TResponse.</returns>
    public async ValueTask<TResponse?> PatchAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default, params (string, string)[]? queryParameters)
        where TRequest : notnull
    {
        var config = GetHttpConfig<TResponse, HttpPatchAttribute>();
        var uri = GetUri(config, null, queryParameters);
        var client = CreateHttpClient(config, uri);
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        var response = await DeserializeResponse<TResponse>(await client.PatchAsync(uri, content, ct), config.FromJsonProperty);
        return response;
    }

    /// <summary>
    /// Sends HTTP DELETE request.
    /// </summary>
    /// <typeparam name="TData">Desired data type to recieve and send.</typeparam>
    /// <param name="data">Data to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task DeleteAsync<TData>(TData data, CancellationToken ct = default, params (string, string)[]? queryParameters)
    {
        var config = GetHttpConfig<TData, HttpDeleteAttribute>();
        var arguments = GetRequestArguments(data, config);
        var uri = GetUri(config, arguments, queryParameters);
        var client = CreateHttpClient(config, uri);
        await client.DeleteAsync(uri, ct);
    }

    /// <summary>
    /// Deserializes JSON reponse message and handles unsuccessful responses.
    /// </summary>
    /// <typeparam name="TResponse">Type to deserialize into.</typeparam>
    /// <param name="responseMessage">The response message.</param>
    /// <param name="fromJsonProperty">(Optional) the JSON property that we must use for deserialization.</param>
    /// <exception cref="HttpClientException">Thrown when HTTP request was not successful.</exception>
    private static async ValueTask<TResponse?> DeserializeResponse<TResponse>(HttpResponseMessage? responseMessage, string? fromJsonProperty)
    {
        if (responseMessage != null)
        {
            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new HttpClientException((int)responseMessage.StatusCode, responseMessage.StatusCode.ToString());
            }
            if (fromJsonProperty != null)
            {
                using JsonDocument document = JsonDocument.Parse(await responseMessage.Content.ReadAsStringAsync());
                if (document.RootElement.TryGetProperty(fromJsonProperty, out JsonElement jsonElement))
                {
                    return JsonSerializer.Deserialize<TResponse>(jsonElement.GetRawText(), _jsonSerializerOptions);
                }
                else
                {
                    throw new JsonException($"Property '{fromJsonProperty}' not found in JSON.");
                }
            }
            return await responseMessage.Content.ReadFromJsonAsync<TResponse>();
        }

        return default;
    }

    /// <summary>
    /// Extracts request arguments of data based on <see cref="HttpAttribute"/> and <see cref="RequestArgumentAttribute"/> configuration.
    /// </summary>
    /// <typeparam name="TRequest">Type of data to use.</typeparam>
    /// <param name="data">Data object.</param>
    /// <param name="config"><see cref="HttpAttribute"/> configuration.</param>
    /// <returns>Array of request arguments.</returns>
    /// <exception cref="NullReferenceException"></exception>
    private static object[]? GetRequestArguments<TRequest>(TRequest data, HttpAttribute config)
    {
        if (data == null)
        {
            return null;
        }

        var arguments = new List<object>();
        IEnumerable<PropertyInfo> argumentProperties = typeof(TRequest).GetProperties().Where(p => config?.ArgumentProperties?.Contains(p) ?? false);
        foreach (var property in argumentProperties)
        {
            var attribute = property.GetCustomAttribute<RequestArgumentAttribute>(false)
                ?? throw new NullReferenceException($"Property {property.Name} in Type {typeof(TRequest).FullName} is not {nameof(RequestArgumentAttribute)}.");
            var value = property.GetValue(data);
            if (value != null)
            {
                arguments.Insert(attribute.Index, value);
            }
        }

        return arguments.ToArray();
    }

    /// <summary>
    /// Gets the requested <see cref="HttpAttribute"/> or default one if exist. Otherwise throws exception.
    /// </summary>
    /// <typeparam name="TData">Data type for cache lookup.</typeparam>
    /// <typeparam name="TAttribute">Type of <see cref="HttpAttribute"/>, you can use its inheritors.</typeparam>
    private static TAttribute GetHttpConfig<TData, TAttribute>()
        where TAttribute : HttpAttribute
    {
        if (!_httpConfigCache.TryGetValue(typeof(TData), out var configs))
        {
            throw new KeyNotFoundException($"Class '{typeof(TData).FullName}' does not have Attribute '{typeof(TAttribute).FullName}' associated!");
        }

        var defaultAttribute = configs.SingleOrDefault(c => c.GetType() == typeof(HttpAttribute));
        
        TAttribute? defaultValue = null;
        if(defaultAttribute != null)
        {
            defaultValue = Activator.CreateInstance<TAttribute>();
            defaultValue.ApiEndpoint = defaultAttribute.ApiEndpoint;
            defaultValue.Headers = defaultAttribute.Headers;
            defaultValue.Authorization = defaultAttribute?.Authorization;
            defaultValue.ArgumentProperties = defaultAttribute?.ArgumentProperties;
            defaultValue.RequestType = defaultAttribute?.RequestType;
            defaultValue.ResponseType = defaultAttribute?.ResponseType;
        }

        var result = (TAttribute?)configs.SingleOrDefault(attr => attr?.GetType() == typeof(TAttribute), defaultValue);

        if (result == null)
        {
            throw new Exception($"{typeof(TData).FullName} does not have {typeof(TAttribute).FullName} or default {nameof(HttpAttribute)} defined! At least one is expected!");
        }

        return result;
    }

    private string GetUri<TConfig>(TConfig config, object[]? requestArguments, (string, string)[]? queryParameters)
        where TConfig : HttpAttribute
    {
        int argLength = config.ArgumentProperties?.Length ?? 0;
        if (argLength == 0)
        {
            argLength = requestArguments?.Length ?? 0;
        }
        StringBuilder sb = new StringBuilder();
        if (config.ArgumentProperties == null || argLength == 0)
        {
            sb.Append(config.ApiEndpoint);
        }
        else
        {
            sb.Append(string.Format(config.ApiEndpoint, requestArguments ?? Array.Empty<object>()));
        }

        if (queryParameters != null)
        {
            if (!sb.ToString().Contains("?"))
            {
                sb.Append("?");
            }
            else
            {
                sb.Append("&");
            }
            for (int i = 0; i < queryParameters.Length; i++)
            {
                sb.Append(queryParameters[i].Item1);
                sb.Append('=');
                sb.Append(queryParameters[i].Item2);
                if (i + 1 < queryParameters.Length)
                {
                    sb.Append("&");
                }
            }
        }
        return sb.ToString();

    }

    /// <summary>
    /// Generates endpoint and applies headers to HttpClient.
    /// </summary>
    /// <typeparam name="TConfig">Type of HTTP config.</typeparam>
    /// <param name="uri">The endpoint to use.</param>
    private HttpClient CreateHttpClient<TConfig>(TConfig config, string uri)
        where TConfig : HttpAttribute
    {
        // Apply headers
        var client = _httpClientFactory.CreateClient();
        foreach (var header in config.Headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
        return client;
    }

    /// <summary>
    /// Used to get the required arguments count from ApiEndpoint string.
    /// </summary>
    /// <param name="input">String, should be the ApiEndpoint.</param>
    /// <returns>The number of or required arguments</returns>
    private static int GetRequiredArguments(string input)
    {
        int count = 0;
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '{' && i + 1 < input.Length && input.IndexOf('}', i) > i)
            {
                count++;
                i = input.IndexOf('}', i); // Move index to the closing brace
            }
        }
        return count;
    }
}
