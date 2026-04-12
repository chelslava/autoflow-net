// =============================================================================
// ServiceCollectionExtensions.cs — методы расширения для регистрации сервисов БД.
//
// Предоставляет удобные методы для подключения SQLite к DI-контейнеру.
// =============================================================================

using System;
using AutoFlow.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AutoFlow.Database;

/// <summary>
/// Методы расширения для регистрации сервисов базы данных.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет SQLite-репозиторий для хранения результатов выполнения.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="databasePath">Путь к файлу базы данных.</param>
    /// <returns>Коллекция сервисов.</returns>
    public static IServiceCollection AddAutoFlowDatabase(
        this IServiceCollection services,
        string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        services.AddSingleton<IExecutionRepository>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SQLiteExecutionRepository>>();
            return new SQLiteExecutionRepository(databasePath, logger);
        });

        services.AddSingleton<IWorkflowLifecycleHook, DatabaseHook>();

        return services;
    }

    /// <summary>
    /// Добавляет SQLite-репозиторий с путём по умолчанию.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <returns>Коллекция сервисов.</returns>
    public static IServiceCollection AddAutoFlowDatabase(this IServiceCollection services)
    {
        var defaultPath = GetDefaultDatabasePath();
        return services.AddAutoFlowDatabase(defaultPath);
    }

    /// <summary>
    /// Возвращает путь к базе данных по умолчанию.
    /// </summary>
    public static string GetDefaultDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var autoflowDir = System.IO.Path.Combine(appDataPath, "AutoFlow");

        if (!System.IO.Directory.Exists(autoflowDir))
            System.IO.Directory.CreateDirectory(autoflowDir);

        return System.IO.Path.Combine(autoflowDir, "autoflow.db");
    }
}
