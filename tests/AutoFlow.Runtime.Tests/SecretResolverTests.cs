using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using AutoFlow.Runtime;
using AutoFlow.Runtime.Secrets;
using Xunit;

namespace AutoFlow.Runtime.Tests;

public sealed class SecretResolverTests
{
    private readonly SecretMasker _masker;
    private readonly List<ISecretProvider> _providers;

    public SecretResolverTests()
    {
        _masker = new SecretMasker();
        _providers = new List<ISecretProvider>();
    }

    [Fact]
    public async Task ResolveAsync_NoSecrets_ReturnsOriginalString()
    {
        var resolver = new SecretResolver(_providers, _masker);
        var input = "Hello, World!";

        var result = await resolver.ResolveAsync(input);

        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public async Task ResolveAsync_EmptyString_ReturnsEmptyString()
    {
        var resolver = new SecretResolver(_providers, _masker);

        var result = await resolver.ResolveAsync("");

        Assert.Equal("", result);
    }

    [Fact]
    public async Task ResolveAsync_NullString_ReturnsEmptyString()
    {
        var resolver = new SecretResolver(_providers, _masker);

        var result = await resolver.ResolveAsync(null!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task ResolveAsync_WithSecretRef_ResolvesSecret()
    {
        _providers.Add(new TestSecretProvider("MY_SECRET", "secret_value_123"));
        var resolver = new SecretResolver(_providers, _masker);

        var result = await resolver.ResolveAsync("Password: ${secret:MY_SECRET}");

        Assert.Equal("Password: secret_value_123", result);
    }

    [Fact]
    public async Task ResolveAsync_MultipleSecrets_ResolvesAll()
    {
        _providers.Add(new TestSecretProvider("USER", "admin"));
        _providers.Add(new TestSecretProvider("PASS", "password123"));
        var resolver = new SecretResolver(_providers, _masker);

        var result = await resolver.ResolveAsync("User: ${secret:USER}, Pass: ${secret:PASS}");

        Assert.Equal("User: admin, Pass: password123", result);
    }

    [Fact]
    public async Task ResolveAsync_UnknownSecret_LeavesUnchanged()
    {
        var resolver = new SecretResolver(_providers, _masker);

        var result = await resolver.ResolveAsync("Key: ${secret:UNKNOWN}");

        Assert.Equal("Key: ${secret:UNKNOWN}", result);
    }

    [Fact]
    public async Task ResolveAsync_RegistersSecretsForMasking()
    {
        _providers.Add(new TestSecretProvider("SECRET", "my_secret_value"));
        var resolver = new SecretResolver(_providers, _masker);

        await resolver.ResolveAsync("${secret:SECRET}");

        var masked = _masker.Mask("The secret is my_secret_value");
        Assert.Equal("The secret is *****", masked);
    }

    [Fact]
    public async Task ResolveObjectAsync_String_ResolvesSecrets()
    {
        _providers.Add(new TestSecretProvider("API_KEY", "key_123"));
        var resolver = new SecretResolver(_providers, _masker);

        var result = await resolver.ResolveObjectAsync("API: ${secret:API_KEY}");

        Assert.Equal("API: key_123", result);
    }

    [Fact]
    public async Task ResolveObjectAsync_Null_ReturnsNull()
    {
        var resolver = new SecretResolver(_providers, _masker);

        var result = await resolver.ResolveObjectAsync(null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveObjectAsync_Dictionary_ResolvesValues()
    {
        _providers.Add(new TestSecretProvider("TOKEN", "token_abc"));
        var resolver = new SecretResolver(_providers, _masker);

        var dict = new Dictionary<string, object?>
        {
            ["name"] = "test",
            ["token"] = "${secret:TOKEN}"
        };

        var result = await resolver.ResolveObjectAsync(dict) as Dictionary<string, object?>;

        Assert.NotNull(result);
        Assert.Equal("test", result!["name"]);
        Assert.Equal("token_abc", result["token"]);
    }

    [Fact]
    public async Task ResolveObjectAsync_NestedDictionary_ResolvesRecursively()
    {
        _providers.Add(new TestSecretProvider("DB_PASS", "db_secret"));
        var resolver = new SecretResolver(_providers, _masker);

        var dict = new Dictionary<string, object?>
        {
            ["config"] = new Dictionary<string, object?>
            {
                ["password"] = "${secret:DB_PASS}"
            }
        };

        var result = await resolver.ResolveObjectAsync(dict) as Dictionary<string, object?>;

        Assert.NotNull(result);
        var nested = result!["config"] as Dictionary<string, object?>;
        Assert.NotNull(nested);
        Assert.Equal("db_secret", nested!["password"]);
    }

    [Fact]
    public async Task ResolveObjectAsync_List_ResolvesItems()
    {
        _providers.Add(new TestSecretProvider("ITEM", "resolved_item"));
        var resolver = new SecretResolver(_providers, _masker);

        var list = new List<object?> { "plain", "${secret:ITEM}", 42 };

        var result = await resolver.ResolveObjectAsync(list) as List<object?>;

        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);
        Assert.Equal("plain", result[0]);
        Assert.Equal("resolved_item", result[1]);
        Assert.Equal(42, result[2]);
    }

    [Fact]
    public async Task ResolveObjectAsync_NonStringNonCollection_ReturnsOriginal()
    {
        var resolver = new SecretResolver(_providers, _masker);

        var result = await resolver.ResolveObjectAsync(42);

        Assert.Equal(42, result);
    }

    [Fact]
    public void GetMasker_ReturnsMasker()
    {
        var resolver = new SecretResolver(_providers, _masker);

        var result = resolver.GetMasker();

        Assert.Same(_masker, result);
    }

    [Fact]
    public void Constructor_NullMasker_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SecretResolver(_providers, null!));
    }
}

internal sealed class TestSecretProvider : ISecretProvider
{
    private readonly string _name;
    private readonly string _value;

    public TestSecretProvider(string name, string value)
    {
        _name = name;
        _value = value;
    }

    public Task<string?> ResolveAsync(string secretRef, CancellationToken cancellationToken = default)
    {
        if (secretRef == _name)
        {
            return Task.FromResult<string?>(_value);
        }
        return Task.FromResult<string?>(null);
    }

    public bool CanResolve(string secretRef)
    {
        return secretRef == _name;
    }
}
