using Declaro.Net.Attributes;

namespace Declaro.Net.Connection
{
    /// <summary>
    /// Handles authorization for <see cref="HttpAttribute"/> and its inheritors.
    /// </summary>
    public sealed class HttpAuthorization
    {
        public HttpAuthorization(AuthorizationType type, string token)
        {
            Type = type;
            Token = token;
        }


        public AuthorizationType Type { get; }

        public string Token { get; }

        public enum AuthorizationType
        {
            Basic,
            Bearer,
            ApiKey
        }
    }
}
