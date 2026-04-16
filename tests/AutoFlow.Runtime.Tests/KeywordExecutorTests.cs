using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using AutoFlow.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AutoFlow.Runtime.Tests;

public sealed class KeywordExecutorTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KeywordRegistry _registry;

    public KeywordExecutorTests()
    {
        _registry = new KeywordRegistry();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_registry);
        services.AddSingleton<TestKeywordHandler>();
        services.AddSingleton<FailingKeywordHandler>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ExecuteAsync_ValidKeyword_ReturnsSuccess()
    {
        _registry.Register("test.keyword", typeof(TestKeywordHandler), typeof(TestKeywordArgs));

        var executor = new KeywordExecutor(_serviceProvider, _registry);
        var context = CreateExecutionContext();

        var args = new Dictionary<string, object>
        {
            ["message"] = "Hello, World!"
        };

        var result = await executor.ExecuteAsync(context, "step1", "test.keyword", args);

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello, World!", result.Outputs);
    }

    [Fact]
    public async Task ExecuteAsync_NullArgs_CreatesDefaultInstance()
    {
        _registry.Register("test.keyword", typeof(TestKeywordHandler), typeof(TestKeywordArgs));

        var executor = new KeywordExecutor(_serviceProvider, _registry);
        var context = CreateExecutionContext();

        var result = await executor.ExecuteAsync(context, "step1", "test.keyword", null);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Outputs);
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyTypedArgs_UsesDirectly()
    {
        _registry.Register("test.keyword", typeof(TestKeywordHandler), typeof(TestKeywordArgs));

        var executor = new KeywordExecutor(_serviceProvider, _registry);
        var context = CreateExecutionContext();

        var typedArgs = new TestKeywordArgs { Message = "Typed message" };

        var result = await executor.ExecuteAsync(context, "step1", "test.keyword", typedArgs);

        Assert.True(result.IsSuccess);
        Assert.Equal("Typed message", result.Outputs);
    }

    [Fact]
    public async Task ExecuteAsync_FailingKeyword_ReturnsFailure()
    {
        _registry.Register("failing.keyword", typeof(FailingKeywordHandler), typeof(FailingKeywordArgs));

        var executor = new KeywordExecutor(_serviceProvider, _registry);
        var context = CreateExecutionContext();

        var args = new Dictionary<string, object>
        {
            ["errorMessage"] = "Something went wrong"
        };

        var result = await executor.ExecuteAsync(context, "step1", "failing.keyword", args);

        Assert.False(result.IsSuccess);
        Assert.Equal("Something went wrong", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownKeyword_ThrowsInvalidOperationException()
    {
        var executor = new KeywordExecutor(_serviceProvider, _registry);
        var context = CreateExecutionContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            executor.ExecuteAsync(context, "step1", "unknown.keyword", null));
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new KeywordExecutor(null!, _registry));
    }

    [Fact]
    public void Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new KeywordExecutor(_serviceProvider, null!));
    }

    private static IExecutionContext CreateExecutionContext()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        return new ExecutionContext(services);
    }
}

public sealed class TestKeywordArgs
{
    public string Message { get; set; } = string.Empty;
}

[Keyword("test.keyword", Category = "Test", Description = "Test keyword")]
public sealed class TestKeywordHandler : IKeywordHandler<TestKeywordArgs>
{
    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        TestKeywordArgs args,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(KeywordResult.Success(args.Message));
    }
}

public sealed class FailingKeywordArgs
{
    public string ErrorMessage { get; set; } = "Error";
}

[Keyword("failing.keyword", Category = "Test", Description = "Failing test keyword")]
public sealed class FailingKeywordHandler : IKeywordHandler<FailingKeywordArgs>
{
    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        FailingKeywordArgs args,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(KeywordResult.Failure(args.ErrorMessage));
    }
}
