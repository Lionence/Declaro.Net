using Declaro.Net.Connection;
using System.Reflection;

namespace Declaro.Net.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class HttpAttribute : Attribute
    {
        private HttpAuthorization? _authorization;

        /// <summary>
        /// The API endpoint to be used by the request.
        /// Supports templating.
        /// </summary>
        public string ApiEndpoint { get; set; }

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
    }
}
