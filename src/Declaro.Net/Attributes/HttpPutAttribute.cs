namespace Declaro.Net.Attributes
{
    // <summary>
    /// You can use this attribute in combination with <see cref="HttpAttribute"/> for specific settings over PUT requests.
    /// </summary>
    public sealed class HttpPutAttribute : HttpAttribute
    {
        public HttpPutAttribute(string? apiEndpoint = null, string? apiEndpointEnvVarName = null, string? apiEndpointConfigurationKey = null) : base(apiEndpoint, apiEndpointEnvVarName, apiEndpointConfigurationKey) { }
    }
}
