using Declaro.Net.Attributes;
using System.Reflection;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Declaro.Net.Examples;

namespace Declaro.Net.Connection
{
    public sealed class HttpService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        private static readonly ReadOnlyDictionary<Type, HttpAttribute[]> _httpConfigCache;

        // Builds _HttpConfigCache from all types of the assembly, that have any of the HttpAttributes defined.
        static HttpService()
        {
            var httpConfigCache = new Dictionary<Type, HttpAttribute[]>();

            Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => type.GetCustomAttributes<HttpAttribute>(true) != null).ToList()
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
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Sends HTTP Delete request.
        /// </summary>
        /// <typeparam name="TData">Desired data type to recieve and send.</typeparam>
        /// <param name="data">Data to delete.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task DeleteAsync<TData>(TData data, CancellationToken ct = default)
        {
            var config = GetHttpConfig<TData, HttpDeleteAttribute>();
            var arguments = GetArguments(data, config);
            ApplyHttpConfig(config, arguments, out var uri);
            await _httpClient.DeleteAsync(uri, ct);
        }

        /// <summary>
        /// Sends HTTP POST request. 
        /// </summary>
        /// <typeparam name="TData">Desired data type to recieve and send.</typeparam>
        /// <param name="data">The object used in the request.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>With data of desired type.</returns>aram>
        public async ValueTask<TData?> PostAsync<TData>(TData data, CancellationToken ct = default)
            where TData : notnull
            => await PostAsync<TData, TData>(data, ct);

        /// <summary>
        /// HTTP POST request send and retrieve different data types.
        /// </summary>
        /// <typeparam name="TResponse">Desired data type to recieve.</typeparam>
        /// <typeparam name="TRequest">Data type to send.</typeparam>
        /// <param name="data">The object used in the request.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>With data of desired type.</returns>
        public async ValueTask<TResponse?> PostAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default)
            where TRequest : notnull
        {
            var config = GetHttpConfig<TResponse, HttpPostAttribute>();
            var arguments = GetArguments(data, config);
            ApplyHttpConfig(config, arguments, out var uri);
            var response = await DeserializeResponse<TResponse>(await _httpClient.PostAsJsonAsync(uri, data, ct));
            return response;
        }

        /// <summary>
        /// HTTP GET request for the specified resource.
        /// </summary>
        /// <typeparam name="TResponse">Desired resource type to recieve.</typeparam>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="arguments">Argument parameters for request.</param>
        /// <returns>Resource requested for specified type.</returns>
        public async ValueTask<TResponse> GetAsync<TResponse>(CancellationToken ct = default, params object[] arguments)
            where TResponse : class
        {
            var config = GetHttpConfig<TResponse, HttpGetAttribute>();
            var cacheKey = GetCacheKey<TResponse>(config, arguments);

            if (_memoryCache.TryGetValue(cacheKey, out ICacheEntry? cacheEntry))
            {
                return cacheEntry?.Value as TResponse ?? throw new UnreachableException();
            }

            ApplyHttpConfig(config, arguments, out var uri);
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

        private static string GetCacheKey<TResponse>(HttpGetAttribute? httpGetConfig = null, params object[] arguments)
            where TResponse : class
        {
            var config = httpGetConfig ?? GetHttpConfig<TResponse, HttpGetAttribute>();
            return string.Format(config.ApiEndpoint, arguments ?? Array.Empty<object>());
        }

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

        private static object[]? GetArguments<TRequest>(TRequest data, HttpAttribute config)
        {
            if (data == null)
            {
                return null;
            }

            var arguments = new List<object>();
            foreach (var argProp in data.GetType().GetProperties().Where(p => config?.ArgumentProperties?.Contains(p) ?? false))
            {
                var argConfig = argProp.GetCustomAttribute<RequestArgumentAttribute>(false)
                    ?? throw new NullReferenceException($"Property {argProp.Name} in Type {typeof(TRequest).FullName} is not {nameof(RequestArgumentAttribute)}.");
                arguments.Insert(argConfig.Index, data);
            }

            return arguments.ToArray();
        }

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

        private void ApplyHttpConfig<TAttribute>(TAttribute config, object[]? arguments, out string uri)
            where TAttribute : HttpAttribute
        {
            // Generate endpoint
            int? argLength = config.ArgumentProperties?.Length;
            if (config.ArgumentProperties == null || !argLength.HasValue || argLength.Value == 0)
            {
                uri = config.ApiEndpoint;
            }
            else
            {
                uri = string.Format(config.ApiEndpoint, arguments ?? Array.Empty<object>());
            }

            // Apply headers    
            foreach (var header in config.Headers)
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

    }
}
