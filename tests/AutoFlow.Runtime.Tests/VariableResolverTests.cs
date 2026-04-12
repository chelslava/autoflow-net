// Этот код нужен для проверки базовой подстановки переменных.
using AutoFlow.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutoFlow.Runtime.Tests;

public sealed class VariableResolverTests
{
    [Fact]
    public void ResolveObject_ShouldReplaceVariableInString()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var context = new ExecutionContext(services);
        context.SetVariable("name", "AutoFlow");

        var result = VariableResolver.ResolveObject("Привет, ${name}", context);

        Assert.Equal("Привет, AutoFlow", result);
    }

    [Fact]
    public void ResolveObject_ShouldReturnPureVariableValue()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var context = new ExecutionContext(services);
        context.SetVariable("count", 5);

        var result = VariableResolver.ResolveObject("${count}", context);

        Assert.Equal(5, result);
    }
}
