namespace Declaro.Net.Examples
{
    /// <summary>
    /// Exception representing unsuccessful HTTP request.
    /// </summary>
    public class HttpClientException : Exception
    {
        /// <summary>
        /// The status code.
        /// </summary>
        public int StatusCode { get; }

        public HttpClientException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}