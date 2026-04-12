// Этот код нужен для доступа к переменным, runtime state и сервисам во время выполнения workflow.
using System;
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

public interface IExecutionContext
{
    IReadOnlyDictionary<string, object?> Variables { get; }

    IReadOnlyDictionary<string, object?> StepResults { get; }

    IReadOnlyDictionary<string, object?> RuntimeState { get; }

    T? GetVariable<T>(string name);

    object? GetVariable(string name);

    void SetVariable(string name, object? value);

    void SetStepResult(string stepId, object? value);

    object? GetStepResult(string stepId);

    void SetRuntimeState(string key, object? value);

    object? GetRuntimeState(string key);

    IServiceProvider Services { get; }
}
