using Declaro.Net.Attributes;
using System.Text.Json.Serialization;

namespace Declaro.Net.Test.TestDataTypes;

[Http(ApiEndpoint = "api/weather")]
[HttpGet(ApiEndpoint = "api/weather?City={0}&Date={1}", RequestType = typeof(WeatherRequest))]
public class WeatherResponse : IWeatherResponse
{
    public required int Celsius { get; set; }

    public required string? City { get; set; }
}

[Http(ApiEndpoint = "api/weather")]
[HttpGet(ApiEndpoint = "api/weather?City={0}&Date={1}", RequestType = typeof(WeatherRequest), CacheTime = "00:00:03.000")]
public sealed class CachedWeatherResponse : WeatherResponse { }

[HttpDelete(ApiEndpoint = "api/weather")]
public sealed class WeatherRequest : IWeatherRequest
{
    [RequestArgument(0)]
    public required string? City { get; set; }

    [RequestArgument(1)]
    public required string? Date { get; set; }
}

[Http(ApiEndpoint = "api/weather")]
[HttpGet(ApiEndpoint = "api/weather?City={0}&Date={1}")]
public sealed class WeatherRequestResponse : IWeatherResponse, IWeatherRequest
{
    public required int Celsius { get; set; }

    [RequestArgument(0)]
    public required string? City { get; set; }

    [RequestArgument(1)]
    public required string? Date { get; set; }
}

[HttpGet(ApiEndpoint = "api/weather?City={0}")]
public sealed class WeatherWithCityClass
{
    public required int Celsius { get; set; }

    public required CityData CityData { get; set; }

    [RequestArgument(0)]
    public required string? City { get; set; }
}

[HttpGet(ApiEndpoint = "api/weather?City={0}", FromJsonProperty = "cityData")]
public sealed class CityData
{
    [RequestArgument(0)]
    public required string? City { get; set; }

    public required string? Country { get; set; }
}

[HttpGet(ApiEndpoint = "api/cities/{0}")]
public sealed class JsonIgnoreCityData
{
    [JsonIgnore]
    [RequestArgument(0)]
    public string? Id { get; set; }

    public required string? Name { get; set; }

    public required string? Country { get; set; }
}

[HttpGet(ApiEndpoint = "api/cities/{0}")]
public sealed class CityDataWithoutRequestParameterArgument
{
    public required string? Name { get; set; }

    public required string? Country { get; set; }
}