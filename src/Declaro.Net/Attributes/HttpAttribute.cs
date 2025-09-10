using Declaro.Net.Connection;
using System.Reflection;

namespace Declaro.Net.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class HttpAttribute : Attribute
    {
        private HttpAuthorization? _authorization;

        public HttpAttribute(
            string? apiEndpoint = null,
            string? apiEndpointEnvVarName = null,
            string? apiEndpointConfigurationKey = null)
        {
            int suppliedCount = 0;
            var errorMessage = $"Exactly one of " +
#if RELEASE
                    $"{nameof(ApiEndpoint)}, " +
#endif
                    $"{nameof(ApiEndpointEnvVarName)}, " +
                    $"or {nameof(ApiEndpointConfigurationKey)} must be provided.";

            if (!string.IsNullOrWhiteSpace(apiEndpoint))
            {
                suppliedCount++;
#if RELEASE
                ApiEndpoint = apiEndpoint;
#endif
            }
            if (!string.IsNullOrWhiteSpace(apiEndpointEnvVarName))
            {
                if(suppliedCount != 0)
                {
                    throw new Exception(errorMessage);
                }
                suppliedCount++;
                ApiEndpointEnvVarName = apiEndpointEnvVarName;
            }
            if (!string.IsNullOrWhiteSpace(apiEndpointConfigurationKey))
            {
                if (suppliedCount != 0)
                {
                    throw new Exception(errorMessage);
                }
                suppliedCount++;
                ApiEndpointConfigurationKey = apiEndpointConfigurationKey;
            }
        }

        /// <summary>
        /// DROPPING SUPPORT FROM v1.4.0
        /// The API endpoint to be used by the request.
        /// Supports templating.
        /// </summary>
#if DEBUG
        [Obsolete(message: $"Use {nameof(ApiEndpointEnvVarName)} or {nameof(ApiEndpointConfigurationKey)} instead.", error: true)]
#else
        [Obsolete(message: $"Use {nameof(ApiEndpointEnvVarName)} or {nameof(ApiEndpointConfigurationKey)} instead.", error: false)]
#endif
        public string ApiEndpoint { get; set; }

        /// <summary>
        /// Sets which API Endpoint to use based on the supplied name of environment variable.
        /// Supports templating.
        /// </summary>
        public string ApiEndpointEnvVarName { get; init; }

        /// <summary>
        /// Sets which API Endpoint to use based on the supplied Configuration key.
        /// Supports templating.
        /// </summary>
        public string ApiEndpointConfigurationKey { get; init; }

        /// <summary>
        /// Authorization method for the API. Do not specify if not required.
        /// Supported types: <see cref="HttpAuthorization.AuthorizationType"/>.
        /// Automatically ads 'Authprozation: [type] [token]' to <see cref="Headers"/>.
        /// </summary>
        public HttpAuthorization? Authorization
        {
            get => _authorization;
            set
            {
                if (value != null && !Headers.ContainsKey("Authorization"))
                {
                    _authorization = value;
                    Headers.Add("Authorization", $"{_authorization.Type} {_authorization.Token}");
                }
            }
        }

        /// <summary>
        /// Default headers to add to the request.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The request type to be used in HTTP body.
        /// Only specify this when you expect a different return type (e.g. for requests DTOs).
        /// Leave unset when no return value expected or the DTO is both used for request and response.
        /// </summary>
        public virtual Type? RequestType { get; set; }

        /// <summary>
        /// The response type that your request is expected to get.
        /// Only specify this when you expect a different return type (e.g. for requests DTOs).
        /// Leave unset when no return value expected or the DTO is both used for request and response.
        /// </summary>
        public virtual Type? ResponseType { get; set; }

        /// <summary>
        /// Properties to scrape for arguments before making request.
        /// </summary>
        public PropertyInfo[]? ArgumentProperties { get; set; }

        /// <summary>
        /// The JSON Property where this data type must be deserialized from.
        /// </summary>
        public string? FromJsonProperty { get; set; }
    }
}
