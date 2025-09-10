namespace Declaro.Net.Attributes
{
    // <summary>
    /// You can use this attribute in combination with <see cref="HttpAttribute"/> for specific settings over PATH requests.
    /// </summary>
    public sealed class HttpPatchAttribute : HttpAttribute
    {
        public HttpPatchAttribute(string? apiEndpoint = null, string? apiEndpointEnvVarName = null, string? apiEndpointConfigurationKey = null) : base(apiEndpoint, apiEndpointEnvVarName, apiEndpointConfigurationKey) { }
    }
}
