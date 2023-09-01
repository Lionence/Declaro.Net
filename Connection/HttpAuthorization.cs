namespace Declaro.Net.Connection
{
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
