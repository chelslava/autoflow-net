// Этот код нужен для хранения переменных, результатов шагов и runtime state во время выполнения workflow.
using System;
using System.Collections.Generic;
using AutoFlow.Abstractions;

namespace AutoFlow.Runtime;

public sealed class ExecutionContext : IExecutionContext
{
    private readonly Dictionary<string, object?> _variables;
    private readonly Dictionary<string, object?> _stepResults;
    private readonly Dictionary<string, object?> _runtimeState;

    public ExecutionContext(IServiceProvider services, Dictionary<string, object?>? variables = null)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        _variables = variables ?? [];
        _stepResults = [];
        _runtimeState = [];
    }

    public IReadOnlyDictionary<string, object?> Variables => _variables;

    public IReadOnlyDictionary<string, object?> StepResults => _stepResults;

    public IReadOnlyDictionary<string, object?> RuntimeState => _runtimeState;

    public IServiceProvider Services { get; }

    public T? GetVariable<T>(string name)
    {
        var value = GetVariable(name);

        if (value is null)
            return default;

        return value is T typed
            ? typed
            : throw new InvalidOperationException($"Переменная '{name}' имеет несовместимый тип.");
    }

    public object? GetVariable(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя переменной не может быть пустым.", nameof(name));

        return _variables.TryGetValue(name, out var value)
            ? value
            : null;
    }

    public void SetVariable(string name, object? value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя переменной не может быть пустым.", nameof(name));

        _variables[name] = value;
    }

    public void SetStepResult(string stepId, object? value)
    {
        if (string.IsNullOrWhiteSpace(stepId))
            throw new ArgumentException("Идентификатор шага не может быть пустым.", nameof(stepId));

        _stepResults[stepId] = value;
    }

    public object? GetStepResult(string stepId)
    {
        if (string.IsNullOrWhiteSpace(stepId))
            throw new ArgumentException("Идентификатор шага не может быть пустым.", nameof(stepId));

        return _stepResults.TryGetValue(stepId, out var value)
            ? value
            : null;
    }

    public void SetRuntimeState(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Ключ runtime state не может быть пустым.", nameof(key));

        _runtimeState[key] = value;
    }

    public object? GetRuntimeState(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Ключ runtime state не может быть пустым.", nameof(key));

        return _runtimeState.TryGetValue(key, out var value)
            ? value
            : null;
    }
}
