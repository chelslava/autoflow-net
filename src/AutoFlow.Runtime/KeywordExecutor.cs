
using System;
using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Type, System.Reflection.MethodInfo?> _methodCache = new();

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
        _jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        // Allow numbers to be deserialized as strings (YAML parser may convert "1" to int)
        _jsonOptions.Converters.Add(new FlexibleStringConverter());
    }

    public async Task<KeywordResult> ExecuteAsync(
        IExecutionContext executionContext,
        string stepId,
        string keywordName,
        object? rawArgs,
        CancellationToken cancellationToken = default)
    {
        if (!_registry.TryGet(keywordName, out var registration))
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
        catch (InvalidOperationException ex)
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
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or System.Text.Json.JsonException or ArgumentException)
        {
            var maskedArgs = MaskSensitiveData(rawArgs);
            return KeywordResult.Failure(
                $"Failed to bind arguments for keyword '{keywordName}' in step '{stepId}': {ex.Message}. " +
                $"Args type expected: {registration.ArgsType.Name}. " +
                $"Raw args: {maskedArgs}");
        }

        var method = _methodCache.GetOrAdd(registration.HandlerType, 
            t => t.GetMethod("ExecuteAsync"));
            
        if (method is null)
            return KeywordResult.Failure(
                $"Handler '{registration.HandlerType.Name}' for keyword '{keywordName}' is missing ExecuteAsync method. " +
                "This is a bug in the keyword implementation.");

        Task<KeywordResult> typedTask;
        try
        {
        var task = method.Invoke(handler, new object[] { context, typedArgs, cancellationToken })
                   ?? throw new InvalidOperationException("ExecuteAsync returned null");
            
            if (task is not Task<KeywordResult> resultTask)
                return KeywordResult.Failure(
                    $"ExecuteAsync for keyword '{keywordName}' returned unexpected type '{task.GetType().Name}'. " +
                    "Expected Task<KeywordResult>.");

            typedTask = resultTask;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or System.Reflection.TargetInvocationException)
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

        using var jsonDoc = JsonSerializer.SerializeToDocument(rawArgs, _jsonOptions);
        var result = jsonDoc.Deserialize(argsType, _jsonOptions);

        return result ?? throw new InvalidOperationException($"Failed to deserialize to '{argsType.Name}'");
    }
    
    private static readonly System.Text.RegularExpressions.Regex SensitiveDataRegex = new(
        @"""(password|token|secret|api[_-]?key|auth[_-]?token|credential)""\s*:\s*""[^""]*""",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private const int MaxMaskingInputSize = 1024 * 1024;

    private static string MaskSensitiveData(object? data)
    {
        if (data is null)
            return "null";

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });

        if (json.Length > MaxMaskingInputSize)
            return $"[Data too large to mask: {json.Length:N0} bytes]";

        try
        {
            return SensitiveDataRegex.Replace(json, @"""$1"":""***""");
        }
        catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
        {
            return "[Masking timed out - data may contain sensitive values]";
        }
    }
}

/// <summary>
/// Allows JSON numbers, booleans to be deserialized as strings.
/// This handles YAML parser converting "1" to int when target property is string.
/// </summary>
internal sealed class FlexibleStringConverter : System.Text.Json.Serialization.JsonConverter<string>
{
    public override string? Read(
        ref System.Text.Json.Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            System.Text.Json.JsonTokenType.String => reader.GetString(),
            System.Text.Json.JsonTokenType.Number => reader.GetDouble().ToString(),
            System.Text.Json.JsonTokenType.True => "true",
            System.Text.Json.JsonTokenType.False => "false",
            System.Text.Json.JsonTokenType.Null => null,
            _ => throw new System.Text.Json.JsonException($"Cannot convert {reader.TokenType} to string")
        };
    }

    public override void Write(
        System.Text.Json.Utf8JsonWriter writer,
        string value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
