// =============================================================================
// RetryNode.cs — описание retry-политики шага.
//
// Поддерживает фиксированный delay, exponential backoff и conditional retry.
// =============================================================================

using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Политика повторных попыток для шага.
/// </summary>
public sealed class RetryNode
{
    /// <summary>Максимальное количество попыток.</summary>
    public int Attempts { get; init; } = 1;

    /// <summary>
    /// Задержка между попытками. Поддерживает:
    /// - Фиксированная: "1s", "500ms"
    /// - Exponential backoff: "exponential:1s:30s" (начало, максимум)
    /// </summary>
    public string? Delay { get; init; }

    /// <summary>
    /// Тип retry. По умолчанию — фиксированный delay.
    /// </summary>
    public RetryType Type { get; init; } = RetryType.Fixed;

    /// <summary>
    /// Множитель для exponential backoff (по умолчанию 2.0).
    /// </summary>
    public double BackoffMultiplier { get; init; } = 2.0;

    /// <summary>
    /// Максимальная задержка для exponential backoff (например, "1m").
    /// </summary>
    public string? MaxDelay { get; init; }

    /// <summary>
    /// Повторять только при указанных типах исключений.
    /// Пустой список = повторять при любой ошибке.
    /// </summary>
    public List<string> RetryOn { get; init; } = new();

    /// <summary>
    /// Игнорировать указанные типы исключений (не повторять).
    /// </summary>
    public List<string> SkipOn { get; init; } = new();
}

/// <summary>
/// Тип retry delay.
/// </summary>
public enum RetryType
{
    /// <summary>Фиксированная задержка между попытками.</summary>
    Fixed,

    /// <summary>Exponential backoff: delay * multiplier^(attempt-1).</summary>
    Exponential,

    /// <summary>Случайный jitter для избежания thundering herd.</summary>
    Jitter
}
