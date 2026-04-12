// =============================================================================
// ExecutionStatistics.cs — статистика по выполнениям workflows.
//
// Содержит агрегированные данные: количество запусков, успешность, среднее время.
// =============================================================================

using System;

namespace AutoFlow.Database;

/// <summary>
/// Статистика по выполнениям workflows.
/// </summary>
public sealed class ExecutionStatistics
{
    /// <summary>Общее количество запусков.</summary>
    public int TotalRuns { get; set; }

    /// <summary>Количество успешных запусков.</summary>
    public int PassedRuns { get; set; }

    /// <summary>Количество failed запусков.</summary>
    public int FailedRuns { get; set; }

    /// <summary>Процент успешных запусков.</summary>
    public double SuccessRate => TotalRuns > 0 ? (double)PassedRuns / TotalRuns * 100 : 0;

    /// <summary>Средняя длительность выполнения (мс).</summary>
    public double AverageDurationMs { get; set; }

    /// <summary>Минимальная длительность выполнения (мс).</summary>
    public long MinDurationMs { get; set; }

    /// <summary>Максимальная длительность выполнения (мс).</summary>
    public long MaxDurationMs { get; set; }

    /// <summary>Общее количество выполненных шагов.</summary>
    public int TotalSteps { get; set; }

    /// <summary>Период, за который собрана статистика.</summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>Период, за который собрана статистика.</summary>
    public DateTimeOffset? To { get; set; }
}
