using Declaro.Net.Attributes;

namespace Declaro.Net.Test.TestDataTypes
{
    [Http(ApiEndpoint = "api/weather")]
    [HttpGet(ApiEndpoint = "api/weather?City={0}?Date={1}", RequestType = typeof(WeatherRequest))]
    public class WeatherResponse : IWeatherResponse
    {
        public required int Celsius { get; set; }

        public required string? City { get; set; }
    }

    [Http(ApiEndpoint = "api/weather")]
    [HttpGet(ApiEndpoint = "api/weather?City={0}?Date={1}", RequestType = typeof(WeatherRequest), CacheTime = "00:00:03.000")]
    public sealed class WeatherCached : WeatherResponse { }

    [HttpDelete(ApiEndpoint = "api/weather")]
    public sealed class WeatherRequest : IWeatherRequest
    {
        [RequestArgument(0)]
        public required string? City { get; set; }

        [RequestArgument(1)]
        public required string? Date { get; set; }
    }

    [Http(ApiEndpoint = "api/weather")]
    [HttpGet(ApiEndpoint = "api/weather?City={0}?Date={1}")]
    public sealed class WeatherRequestResponse : IWeatherResponse, IWeatherRequest
    {
        public required int Celsius { get; set; }

        [RequestArgument(0)]
        public required string? City { get; set; }

        [RequestArgument(1)]
        public required string? Date { get; set; }
    }
}
