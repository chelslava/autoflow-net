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

        var handler = _serviceProvider.GetRequiredService(registration.HandlerType);
        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

        var context = new KeywordContext
        {
            ExecutionContext = executionContext,
            StepId = stepId,
            KeywordName = keywordName,
            Logger = loggerFactory.CreateLogger(registration.HandlerType)
        };

        var typedArgs = BindArgs(rawArgs, registration.ArgsType);

        var method = registration.HandlerType.GetMethod("ExecuteAsync");
        if (method is null)
            throw new InvalidOperationException($"У обработчика '{registration.HandlerType.Name}' не найден метод ExecuteAsync.");

        var task = method.Invoke(handler, [context, typedArgs!, cancellationToken])
                   ?? throw new InvalidOperationException($"Не удалось вызвать ExecuteAsync у '{registration.HandlerType.Name}'.");

        if (task is not Task<KeywordResult> typedTask)
            throw new InvalidOperationException($"Метод ExecuteAsync у '{registration.HandlerType.Name}' вернул неподдерживаемый тип.");

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

        return result ?? throw new InvalidOperationException($"Не удалось привязать аргументы к типу '{argsType.Name}'.");
    }
}
