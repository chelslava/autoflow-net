// Этот код нужен для автоматической регистрации keyword-обработчиков из сборки.
using System;
using System.Linq;
using System.Reflection;
using AutoFlow.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AutoFlow.PluginModel;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKeywordsFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        Action<string, Type, Type> registerKeyword)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Select(type => new
            {
                Type = type,
                Attribute = type.GetCustomAttribute<KeywordAttribute>(),
                HandlerInterface = type.GetInterfaces()
                    .FirstOrDefault(i =>
                        i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IKeywordHandler<>))
            })
            .Where(x => x.Attribute is not null && x.HandlerInterface is not null)
            .ToList();

        foreach (var item in handlerTypes)
        {
            var argsType = item.HandlerInterface!.GetGenericArguments()[0];

            registerKeyword(item.Attribute!.Name, item.Type, argsType);
            services.AddTransient(item.Type);
        }

        return services;
    }
}
