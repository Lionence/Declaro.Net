namespace Declaro.Net.Attributes
{
    // <summary>
    /// You can use this attribute in combination with <see cref="HttpAttribute"/> for specific settings over POST requests.
    /// </summary>
    public class HttpPostAttribute : HttpAttribute
    {
        public HttpPostAttribute(string? apiEndpoint = null, string? apiEndpointEnvVarName = null, string? apiEndpointConfigurationKey = null) : base(apiEndpoint, apiEndpointEnvVarName, apiEndpointConfigurationKey) { }
    }
}
