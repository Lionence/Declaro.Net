using Declaro.Net.Attributes;
using Declaro.Net.Examples;

namespace Declaro.Net.Examples
{
    [Http(ApiEndpoint = Constants.TimeApi)]
    public sealed class Time
    {
        public DateTimeOffset Value { get; set; } = DateTimeOffset.UtcNow;
    }

    [Http(ApiEndpoint = "api/TimeCached")]
    [HttpGet(ApiEndpoint = "api/TimeCached", CacheTime = "00:00:01.000")]
    public sealed class TimeCached
    {
        public DateTimeOffset Value { get; set; } = DateTimeOffset.UtcNow;
    }
}