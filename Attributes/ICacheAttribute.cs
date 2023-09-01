namespace Declaro.Net.Attributes
{
    internal interface ICacheAttribute
    {
        /// <summary>
        /// The time which should be pass between requests to retrieve new information again. Until then, a memory cache is used.
        /// Not setting this property will result in no caching.
        /// </summary>
        string? CacheTime { get; set; }
    }
}
