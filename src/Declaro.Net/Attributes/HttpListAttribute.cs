namespace Declaro.Net.Attributes
{
    // <summary>
    /// You can use this attribute in combination with <see cref="HttpAttribute"/> for specific settings over "LIST" (technically POST) requests.
    /// HTTP "LIST" is the concept of retrieving multiple records with one call,to enable developers narrowing down these use cases.
    /// Technically it's still POST. Don't use it for declaring all POST settings! It should be specific for bulk query!
    /// </summary>
    public sealed class HttpListAttribute : HttpPostAttribute, ICacheAttribute
    {
        /// <summary>
        /// The time which should be pass between requests to retrieve new information again. Until then, a memory cache is used.
        /// Not setting this property will result in no caching.
        /// </summary>
        public string? CacheTime { get; set; }
    }
}
