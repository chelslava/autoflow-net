using System;
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Execution context interface for accessing variables, step results, and services during workflow execution.
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// Workflow variables.
    /// </summary>
    IReadOnlyDictionary<string, object?> Variables { get; }

    /// <summary>
    /// Results from completed steps, keyed by step ID.
    /// </summary>
    IReadOnlyDictionary<string, object?> StepResults { get; }

    /// <summary>
    /// Runtime state for cross-step communication.
    /// </summary>
    IReadOnlyDictionary<string, object?> RuntimeState { get; }

    /// <summary>
    /// Gets a variable value by name.
    /// </summary>
    T? GetVariable<T>(string name);

    /// <summary>
    /// Gets a variable value by name.
    /// </summary>
    object? GetVariable(string name);

    /// <summary>
    /// Sets a variable value.
    /// </summary>
    void SetVariable(string name, object? value);

    /// <summary>
    /// Sets the result of a step.
    /// </summary>
    void SetStepResult(string stepId, object? value);

    /// <summary>
    /// Gets the result of a completed step.
    /// </summary>
    object? GetStepResult(string stepId);

    /// <summary>
    /// Sets a runtime state value.
    /// </summary>
    void SetRuntimeState(string key, object? value);

    /// <summary>
    /// Gets a runtime state value.
    /// </summary>
    object? GetRuntimeState(string key);

    /// <summary>
    /// Service provider for DI access.
    /// </summary>
    IServiceProvider Services { get; }
}
