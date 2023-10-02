namespace Declaro.Net.Attributes
{
    /// <summary>
    /// Associates class properties with request argument index.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RequestArgumentAttribute : Attribute
    {
        public RequestArgumentAttribute(int index)
        {
            Index = index;
        }

        /// <summary>
        /// The index.
        /// </summary>
        public int Index { get; set; }
    }
}
