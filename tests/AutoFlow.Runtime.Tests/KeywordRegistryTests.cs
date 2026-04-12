// Этот код нужен для проверки регистрации и поиска keyword.
using AutoFlow.Runtime;
using Xunit;

namespace AutoFlow.Runtime.Tests;

public sealed class KeywordRegistryTests
{
    [Fact]
    public void Register_ShouldStoreKeyword()
    {
        var registry = new KeywordRegistry();

        registry.Register("log.info", typeof(object), typeof(object));

        var result = registry.Get("log.info");

        Assert.Equal("log.info", result.Name);
    }

    [Fact]
    public void Register_Duplicate_ShouldThrow()
    {
        var registry = new KeywordRegistry();
        registry.Register("log.info", typeof(object), typeof(object));

        Assert.Throws<InvalidOperationException>(() =>
            registry.Register("log.info", typeof(object), typeof(object)));
    }
}
