using Declaro.Net.Connection;
using Microsoft.Extensions.Caching.Memory;
using RichardSzalay.MockHttp;

namespace Declaro.Net.Test.HttpServiceTests.Base
{
    public abstract class HttpServiceTestBase
    {
        protected readonly MockHttpMessageHandler _MockHandler = new MockHttpMessageHandler();

        protected readonly IMemoryCache _MemoryCache = new MemoryCache(new MemoryCacheOptions());

        protected abstract HttpService _HttpService { get; }

        protected readonly Type _HttpServiceType = typeof(HttpService);
    }
}
