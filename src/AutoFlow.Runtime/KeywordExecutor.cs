// Этот код нужен для динамического вызова keyword-обработчиков с типизированными аргументами.
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Runtime;

public sealed class KeywordExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KeywordRegistry _registry;
    private readonly JsonSerializerOptions _jsonOptions;

    public KeywordExecutor(
        IServiceProvider serviceProvider,
        KeywordRegistry registry)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<KeywordResult> ExecuteAsync(
        IExecutionContext executionContext,
        string stepId,
        string keywordName,
        object? rawArgs,
        CancellationToken cancellationToken = default)
    {
        var registration = _registry.Get(keywordName);

        if (registration is null)
        {
            var availableKeywords = string.Join(", ", _registry.GetAll().Take(10).Select(k => k.Name));
            var more = _registry.GetAll().Count > 10 ? $" (and {_registry.GetAll().Count - 10} more)" : "";
            return KeywordResult.Failure(
                $"Unknown keyword '{keywordName}' in step '{stepId}'. " +
                $"Available keywords: {availableKeywords}{more}. " +
                "Check 'uses' field spelling or register the keyword.");
        }

        object handler;
        try
        {
            handler = _serviceProvider.GetRequiredService(registration.HandlerType);
        }
        catch (Exception ex) when (ex is not null)
        {
            return KeywordResult.Failure(
                $"Failed to resolve handler for keyword '{keywordName}' in step '{stepId}': {ex.Message}. " +
                "Ensure the keyword is registered in DI container.");
        }

        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

        var context = new KeywordContext
        {
            ExecutionContext = executionContext,
            StepId = stepId,
            KeywordName = keywordName,
            Logger = loggerFactory.CreateLogger(registration.HandlerType)
        };

        object typedArgs;
        try
        {
            typedArgs = BindArgs(rawArgs, registration.ArgsType) ?? Activator.CreateInstance(registration.ArgsType)!;
        }
        catch (Exception ex) when (ex is not null)
        {
            var argsPreview = rawArgs is null 
                ? "null" 
                : JsonSerializer.Serialize(rawArgs, new JsonSerializerOptions { WriteIndented = false });
            return KeywordResult.Failure(
                $"Failed to bind arguments for keyword '{keywordName}' in step '{stepId}': {ex.Message}. " +
                $"Args type expected: {registration.ArgsType.Name}. " +
                $"Raw args: {argsPreview}");
        }

        var method = registration.HandlerType.GetMethod("ExecuteAsync");
        if (method is null)
            return KeywordResult.Failure(
                $"Handler '{registration.HandlerType.Name}' for keyword '{keywordName}' is missing ExecuteAsync method. " +
                "This is a bug in the keyword implementation.");

        Task<KeywordResult> typedTask;
        try
        {
            var task = method.Invoke(handler, [context, typedArgs, cancellationToken])
                       ?? throw new InvalidOperationException("ExecuteAsync returned null");
            
            if (task is not Task<KeywordResult> resultTask)
                return KeywordResult.Failure(
                    $"ExecuteAsync for keyword '{keywordName}' returned unexpected type '{task.GetType().Name}'. " +
                    "Expected Task<KeywordResult>.");

            typedTask = resultTask;
        }
        catch (Exception ex) when (ex is not null)
        {
            var innerEx = ex is System.Reflection.TargetInvocationException tie ? tie.InnerException : ex;
            return KeywordResult.Failure(
                $"Keyword '{keywordName}' threw exception in step '{stepId}': {innerEx?.Message ?? ex.Message}. " +
                $"Handler: {registration.HandlerType.Name}.");
        }

        return await typedTask.ConfigureAwait(false);
    }

    private object? BindArgs(object? rawArgs, Type argsType)
    {
        if (rawArgs is null)
            return Activator.CreateInstance(argsType);

        if (argsType.IsInstanceOfType(rawArgs))
            return rawArgs;

        var json = JsonSerializer.Serialize(rawArgs, _jsonOptions);
        var result = JsonSerializer.Deserialize(json, argsType, _jsonOptions);

        return result ?? throw new InvalidOperationException($"Failed to deserialize to '{argsType.Name}'");
    }
}
