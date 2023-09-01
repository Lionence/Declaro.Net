namespace Declaro.Net.Examples
{
    public class HttpClientException : Exception
    {
        public int StatusCode { get; }

        public HttpClientException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}