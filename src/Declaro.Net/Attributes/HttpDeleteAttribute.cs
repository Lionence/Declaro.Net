using System.Net;

namespace Declaro.Net.Attributes
{
    // <summary>
    /// You can use this attribute in combination with <see cref="HttpAttribute"/> for specific settings over DELETE requests.
    /// </summary>
    public sealed class HttpDeleteAttribute : HttpAttribute
    {
        public HttpDeleteAttribute(string? apiEndpoint = null, string? apiEndpointEnvVarName = null, string? apiEndpointConfigurationKey = null) : base(apiEndpoint, apiEndpointEnvVarName, apiEndpointConfigurationKey) { }
    }
}
