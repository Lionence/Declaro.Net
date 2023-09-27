using Declaro.Net.Attributes;
using System.Reflection;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Declaro.Net.Exceptions;

namespace Declaro.Net.Connection
{
    /// <summary>
    /// Service that handles HTTP requests for Declaro.NET based on implicit definitions.
    /// </summary>
    public sealed class HttpService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        private static readonly ReadOnlyDictionary<Type, HttpAttribute[]> _httpConfigCache;

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

        public HttpService(HttpClient httpClient, IMemoryCache memoryCache)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(HttpClient));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(IMemoryCache));
        }

        /// <summary>
        /// Sends HTTP GET request.
        /// </summary>
        /// <typeparam name="TResponse">Data type to recieve.</typeparam>
        /// <typeparam name="TRequest">Data type to send.</typeparam>
        /// <param name="data">Data object to send.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Resource requested for specified type.</returns>
        public async ValueTask<TResponse> GetAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default)
            where TResponse : class
        {
            var config = GetHttpConfig<TResponse, HttpGetAttribute>();
            var queryParameters = GetQueryParameters(data, config);
            return await GetAsync<TResponse>(ct, queryParameters);
        }

        /// <summary>
        /// Sends HTTP GET request.
        /// </summary>
        /// <typeparam name="TResponse">Data type to recieve.</typeparam>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="queryParameters">Query parameters.</param>
        /// <returns>Resource requested for specified type.</returns>
        public async ValueTask<TResponse> GetAsync<TResponse>(CancellationToken ct = default, params object[]? queryParameters)
            where TResponse : class
        {
            var config = GetHttpConfig<TResponse, HttpGetAttribute>();
            var cacheKey = GetCacheKey<TResponse, HttpGetAttribute>(config, queryParameters);

            if (_memoryCache.TryGetValue(cacheKey, out ICacheEntry? cacheEntry))
            {
                return cacheEntry?.Value as TResponse ?? throw new UnreachableException();
            }

            ApplyHttpConfig(config, queryParameters, out var uri);
            var responseMessage = await _httpClient.GetAsync(uri, ct);
            var response = await DeserializeResponse<TResponse>(responseMessage);

            TimeSpan.TryParse(config?.CacheTime, out var cachingTime);
            if (cachingTime > TimeSpan.Zero)
            {
                cacheEntry = _memoryCache.CreateEntry(cacheKey);
                cacheEntry.Value = response;
                _memoryCache.Set(cacheKey, cacheEntry, cachingTime);
            }

            return response ?? throw new NullReferenceException($"Empty response for GET: {typeof(TResponse).Name} on endpoint '{config?.ApiEndpoint}'");
        }

        /// <summary>
        /// BULK query handling, technically an HTTP POST request.
        /// </summary>
        /// <typeparam name="TData">Data type to both recieve and send.</typeparam>
        /// <param name="data">Object to send.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>With data of desired type - TResponse.</returns>
        public async ValueTask<TData?> ListAsync<TData>(TData data, CancellationToken ct = default)
            where TData : class
            => await ListAsync<TData, TData>(data, ct);

        /// <summary>
        /// BULK query handling, technically an HTTP POST request.
        /// </summary>
        /// <typeparam name="TResponse">Data type to recieve.</typeparam>
        /// <typeparam name="TRequest">Data type to send.</typeparam>
        /// <param name="data">Object to send.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>With data of desired type - TResponse.</returns>
        public async ValueTask<TResponse?> ListAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default)
            where TRequest : notnull
            where TResponse : class
        {
            var config = GetHttpConfig<TResponse, HttpListAttribute>();
            var cacheKey = GetCacheKey<TResponse, HttpListAttribute>(config, null);

            if (_memoryCache.TryGetValue(cacheKey, out ICacheEntry? cacheEntry))
            {
                return cacheEntry?.Value as TResponse ?? throw new UnreachableException();
            }

            ApplyHttpConfig(config, null, out var uri);
            var response = await DeserializeResponse<TResponse>(await _httpClient.PostAsJsonAsync(uri, data, ct));

            TimeSpan.TryParse(config?.CacheTime, out var cachingTime);
            if (cachingTime > TimeSpan.Zero)
            {
                cacheEntry = _memoryCache.CreateEntry(cacheKey);
                cacheEntry.Value = response;
                _memoryCache.Set(cacheKey, cacheEntry, cachingTime);
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
        public async ValueTask<TData?> PostAsync<TData>(TData data, CancellationToken ct = default)
            where TData : notnull
            => await PostAsync<TData, TData>(data, ct);

        /// <summary>
        /// Sends HTTP POST request. 
        /// </summary>
        /// <typeparam name="TResponse">Data type to recieve.</typeparam>
        /// <typeparam name="TRequest">Data type to send.</typeparam>
        /// <param name="data">Object to send.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>With data of desired type - TResponse.</returns>
        public async ValueTask<TResponse?> PostAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default)
            where TRequest : notnull
        {
            var config = GetHttpConfig<TResponse, HttpPostAttribute>();
            ApplyHttpConfig(config, null, out var uri);
            var response = await DeserializeResponse<TResponse>(await _httpClient.PostAsJsonAsync(uri, data, ct));
            return response;
        }

        /// <summary>
        /// Sends HTTP PUT request.
        /// </summary>
        /// <typeparam name="TData">Data type to both recieve and send.</typeparam>
        /// <param name="data">Object to send.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>With data of desired type - TResponse.</returns>
        public async ValueTask<TData?> PutAsync<TData>(TData data, CancellationToken ct = default)
            where TData : notnull
            => await PutAsync<TData, TData>(data, ct);

        /// <summary>
        /// Sends HTTP PUT request. 
        /// </summary>
        /// <typeparam name="TResponse">Data type to recieve.</typeparam>
        /// <typeparam name="TRequest">Data type to send.</typeparam>
        /// <param name="data">Object to send.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>With data of desired type - TResponse.</returns>
        public async ValueTask<TResponse?> PutAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default)
            where TRequest : notnull
        {
            var config = GetHttpConfig<TResponse, HttpPutAttribute>();
            ApplyHttpConfig(config, null, out var uri);
            var response = await DeserializeResponse<TResponse>(await _httpClient.PutAsJsonAsync(uri, data, ct));
            return response;
        }

        /// <summary>
        /// Sends HTTP PATCH request.
        /// </summary>
        /// <typeparam name="TData">Data type to both recieve and send.</typeparam>
        /// <param name="data">Object to send.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>With data of desired type - TResponse.</returns>
        public async ValueTask<TData?> PatchAsync<TData>(TData data, CancellationToken ct = default)
            where TData : notnull
            => await PatchAsync<TData, TData>(data, ct);

        /// <summary>
        /// Sends HTTP PATCH request. 
        /// </summary>
        /// <typeparam name="TResponse">Data type to recieve.</typeparam>
        /// <typeparam name="TRequest">Data type to send.</typeparam>
        /// <param name="data">Object to send.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>With data of desired type - TResponse.</returns>
        public async ValueTask<TResponse?> PatchAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default)
            where TRequest : notnull
        {
            var config = GetHttpConfig<TResponse, HttpPatchAttribute>();
            ApplyHttpConfig(config, null, out var uri);
            var response = await DeserializeResponse<TResponse>(await _httpClient.PatchAsJsonAsync(uri, data, ct));
            return response;
        }

        /// <summary>
        /// Sends HTTP DELETE request.
        /// </summary>
        /// <typeparam name="TData">Desired data type to recieve and send.</typeparam>
        /// <param name="data">Data to delete.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task DeleteAsync<TData>(TData data, CancellationToken ct = default)
        {
            var config = GetHttpConfig<TData, HttpDeleteAttribute>();
            var arguments = GetQueryParameters(data, config);
            ApplyHttpConfig(config, arguments, out var uri);
            await _httpClient.DeleteAsync(uri, ct);
        }

        /// <summary>
        /// Retrieves HTTP response cache key based on config, desied response type and exact API Endpoint.
        /// </summary>
        /// <typeparam name="TResponse">Requested data type.</typeparam>
        /// <param name="httpConfig"></param>
        /// <param name="queryParameters"></param>
        /// <returns></returns>
        private static string GetCacheKey<TResponse, TConfig>(TConfig? httpConfig = null, params object[]? queryParameters)
            where TResponse : class
            where TConfig : HttpAttribute, ICacheAttribute
        {
            var config = httpConfig ?? GetHttpConfig<TResponse, TConfig>();
            return string.Format(config.ApiEndpoint, queryParameters ?? Array.Empty<object>());
        }

        /// <summary>
        /// Deserializes JSON reponse message and handles unsuccessful responses.
        /// </summary>
        /// <typeparam name="TResponse">Type to deserialize into.</typeparam>
        /// <param name="responseMessage">The response message.</param>
        /// <exception cref="HttpClientException">Thrown when HTTP request was not successful.</exception>
        private static async ValueTask<TResponse?> DeserializeResponse<TResponse>(HttpResponseMessage? responseMessage)
        {
            if (responseMessage != null)
            {
                if (!responseMessage.IsSuccessStatusCode)
                {
                    throw new HttpClientException((int)responseMessage.StatusCode, responseMessage.StatusCode.ToString());
                }

                int messageLength = (await responseMessage.Content.ReadAsByteArrayAsync()).Length;
                if (messageLength > 0)
                {
                    return await responseMessage.Content.ReadFromJsonAsync<TResponse>();
                }
            }

            return default;
        }

        /// <summary>
        /// Extracts query parameters of data based on <see cref="HttpAttribute"/> and <see cref="RequestArgumentAttribute"/> configuration.
        /// </summary>
        /// <typeparam name="TRequest">Type of data to use.</typeparam>
        /// <param name="data">Data object.</param>
        /// <param name="config"><see cref="HttpAttribute"/> configuration.</param>
        /// <returns>Array of query parameters.</returns>
        /// <exception cref="NullReferenceException"></exception>
        private static object[]? GetQueryParameters<TRequest>(TRequest data, HttpAttribute config)
        {
            if (data == null)
            {
                return null;
            }

            var arguments = new List<object>();
            foreach (var argProp in typeof(TRequest).GetProperties().Where(p => config?.ArgumentProperties?.Contains(p) ?? false))
            {
                var argConfig = argProp.GetCustomAttribute<RequestArgumentAttribute>(false)
                    ?? throw new NullReferenceException($"Property {argProp.Name} in Type {typeof(TRequest).FullName} is not {nameof(RequestArgumentAttribute)}.");
                var value = argProp.GetValue(data);
                if (value != null)
                {
                    arguments.Insert(argConfig.Index, value);
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
                defaultValue.Authorization = defaultAttribute?.Authorization;
                defaultValue.ArgumentProperties = defaultAttribute?.ArgumentProperties;
                defaultValue.Headers = defaultAttribute?.Headers ?? new Dictionary<string, string>();
                defaultValue.ApiEndpoint = defaultAttribute?.ApiEndpoint ?? string.Empty;
                defaultValue.RequestType = defaultAttribute?.RequestType;
                defaultValue.ResponseType = defaultAttribute?.ResponseType;
            }

            var result = (TAttribute?)configs.SingleOrDefault(attr => attr is TAttribute, defaultValue);

            if (result == null)
            {
                throw new Exception($"{typeof(TData).FullName} does not have {typeof(TAttribute).FullName} or default {nameof(HttpAttribute)} defined! At least one is expected!");
            }

            return result;
        }

        /// <summary>
        /// Generates endpoint and applies headers to HttpClient.
        /// </summary>
        /// <typeparam name="TConfig">Type of HTTP config.</typeparam>
        /// <param name="config">HTTP config object.</param>
        /// <param name="queryParameters">Query parameters.</param>
        /// <param name="uri">Output parameter with the exact endpoint to use.</param>
        private void ApplyHttpConfig<TConfig>(TConfig config, object[]? queryParameters, out string uri)
            where TConfig : HttpAttribute
        {
            // Generate endpoint
            int? argLength = config.ArgumentProperties?.Length;
            if (config.ArgumentProperties == null || !argLength.HasValue || argLength.Value == 0)
            {
                uri = config.ApiEndpoint;
            }
            else
            {
                uri = string.Format(config.ApiEndpoint, queryParameters ?? Array.Empty<object>());
            }

            // Apply headers    
            foreach (var header in config.Headers)
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

    }
}
