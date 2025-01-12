using Declaro.Net.Attributes;

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
[HttpGet(ApiEndpoint = "api/weather?City={0}&Date={1}")]
public sealed class WeatherRequestResponse : IWeatherResponse, IWeatherRequest
{
    public required int Celsius { get; set; }

    [RequestArgument(0)]
    public required string? City { get; set; }

    [RequestArgument(1)]
    public required string? Date { get; set; }
}

[HttpGet(ApiEndpoint = "umbraco/delivery/api/v2/content/item/{0}", CacheTime = "10:00")]
public class Website
{
    public string ContentType { get; set; }
    public string Name { get; set; }

    [RequestArgument(0)]
    public Guid Id { get; set; }
    public string ErrorNotFoundHeader { get; set; }
    public string ErrorNotFoundDescription { get; set; }
    public string ErrorNotFoundButtonText { get; set; }
    public string ErrorNotFoundButtonHref { get; set; }
    public string ErrorOtherHeader { get; set; }
    public string ErrorOtherDescription { get; set; }
    public string ErrorOtherButtonText { get; set; }
    public string ErrorOtherButtonHref { get; set; }
    public string BaseTitle { get; set; }
    public List<Favicon> Favicon { get; set; }
    public string Description { get; set; }
    public string Keywords { get; set; }
    public string SeoRobotsMetaTag { get; set; }
}

public class Favicon
{
    public Guid Id { get; set; }
    public string Url { get; set; }
}