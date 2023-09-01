namespace Declaro.Net.Attributes
{
    // <summary>
    /// You can use this attribute in combination with <see cref="HttpAttribute"/> for specific settings over GET requests.
    /// </summary>
    public sealed class HttpGetAttribute : HttpAttribute, ICacheAttribute
    {
        /// <summary>
        /// The time which should be pass between requests to retrieve new information again. Until then, a memory cache is used.
        /// Not setting this property will result in no caching.
        /// </summary>
        public string? CacheTime { get; set; }
    }
}
