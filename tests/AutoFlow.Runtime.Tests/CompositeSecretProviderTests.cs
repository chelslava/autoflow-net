using System;
using System.Collections.Generic;
using AutoFlow.Abstractions;
using AutoFlow.Runtime.Secrets;
using Xunit;

namespace AutoFlow.Runtime.Tests;

public sealed class CompositeSecretProviderTests
{
    [Fact]
    public void Constructor_NullProviders_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CompositeSecretProvider(null!));
    }

    [Fact]
    public void Constructor_EmptyProviders_ReturnsEmptyComposite()
    {
        var composite = new CompositeSecretProvider(new ISecretProvider[0]);

        Assert.Empty(composite);
    }

    [Fact]
    public void Constructor_AddsMultipleProviders()
    {
        var provider1 = new TestSecretProvider("SECRET1", "value1");
        var provider2 = new TestSecretProvider("SECRET2", "value2");
        var composite = new CompositeSecretProvider(new[] { provider1, provider2 });

        Assert.True(composite.CanResolve("SECRET1"));
        Assert.True(composite.CanResolve("SECRET2"));
    }

    [Fact]
    public void CanResolve_FirstProviderMatches_ReturnsTrue()
    {
        var provider1 = new TestSecretProvider("FIRST", "value1");
        var provider2 = new TestSecretProvider("SECOND", "value2");
        var composite = new CompositeSecretProvider(new[] { provider1, provider2 });

        Assert.True(composite.CanResolve("FIRST"));
    }

    [Fact]
    public void CanResolve_SecondProviderMatches_ReturnsTrue()
    {
        var provider1 = new TestSecretProvider("FIRST", "value1");
        var provider2 = new TestSecretProvider("SECOND", "value2");
        var composite = new CompositeSecretProvider(new[] { provider1, provider2 });

        Assert.True(composite.CanResolve("SECOND"));
    }

    [Fact]
    public void CanResolve_NoProviderMatches_ReturnsFalse()
    {
        var provider1 = new TestSecretProvider("FIRST", "value1");
        var provider2 = new TestSecretProvider("SECOND", "value2");
        var composite = new CompositeSecretProvider(new[] { provider1, provider2 });

        Assert.False(composite.CanResolve("UNKNOWN"));
    }

    [Fact]
    public async Task ResolveAsync_FirstProviderReturnsValue_ReturnsIt()
    {
        var provider1 = new TestSecretProvider("SECRET", "first_value");
        var provider2 = new TestSecretProvider("SECRET", "second_value");
        var composite = new CompositeSecretProvider(new[] { provider1, provider2 });

        var result = await composite.ResolveAsync("SECRET");

        Assert.Equal("first_value", result);
    }

    [Fact]
    public async Task ResolveAsync_SecondProviderReturnsValue_ReturnsIt()
    {
        var provider1 = new TestSecretProvider("SECRET", null!);
        var provider2 = new TestSecretProvider("SECRET", "second_value");
        var composite = new CompositeSecretProvider(new[] { provider1, provider2 });

        var result = await composite.ResolveAsync("SECRET");

        Assert.Equal("second_value", result);
    }

    [Fact]
    public async Task ResolveAsync_NoProviderReturnsNull_ReturnsNull()
    {
        var provider1 = new TestSecretProvider("SECRET", null!);
        var provider2 = new TestSecretProvider("SECRET", null!);
        var composite = new CompositeSecretProvider(new[] { provider1, provider2 });

        var result = await composite.ResolveAsync("SECRET");

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_SecondProviderCanResolveButFirstCannot_ReturnsSecond()
    {
        var provider1 = new TestSecretProvider("OTHER", "value1");
        var provider2 = new TestSecretProvider("SECRET", "second_value");
        var composite = new CompositeSecretProvider(new[] { provider1, provider2 });

        var result = await composite.ResolveAsync("SECRET");

        Assert.Equal("second_value", result);
    }

    [Fact]
    public void AddProvider_NullProvider_ThrowsArgumentNullException()
    {
        var composite = new CompositeSecretProvider(new ISecretProvider[0]);

        Assert.Throws<ArgumentNullException>(() =>
            composite.AddProvider(null!));
    }

    [Fact]
    public void AddProvider_AddsProviderToChain()
    {
        var composite = new CompositeSecretProvider(new ISecretProvider[0]);
        var provider = new TestSecretProvider("NEW", "new_value");

        composite.AddProvider(provider);

        Assert.True(composite.CanResolve("NEW"));
    }

    [Fact]
    public void AddProvider_MultipleCalls_AddsAllProviders()
    {
        var composite = new CompositeSecretProvider(new ISecretProvider[0]);
        var provider1 = new TestSecretProvider("FIRST", "value1");
        var provider2 = new TestSecretProvider("SECOND", "value2");

        composite.AddProvider(provider1);
        composite.AddProvider(provider2);

        Assert.True(composite.CanResolve("FIRST"));
        Assert.True(composite.CanResolve("SECOND"));
    }

    [Fact]
    public void Priority_FirstProviderTakes precedence()
    {
        var provider1 = new TestSecretProvider("SHARED", "first");
        var provider2 = new TestSecretProvider("SHARED", "second");
        var composite = new CompositeSecretProvider(new[] { provider1, provider2 });

        var result = await composite.ResolveAsync("SHARED");

        Assert.Equal("first", result);
    }
}

internal sealed class TestSecretProvider : ISecretProvider
{
    private readonly string _name;
    private readonly string? _value;

    public TestSecretProvider(string name, string? value)
    {
        _name = name;
        _value = value;
    }

    public Task<string?> ResolveAsync(string secretRef, System.Threading.CancellationToken cancellationToken = default)
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
