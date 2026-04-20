// Этот код нужен для простой подстановки переменных вида ${name} в аргументы шагов.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using AutoFlow.Abstractions;

namespace AutoFlow.Runtime;

public static class VariableResolver
{
    private static readonly Regex VariableRegex =
        new(@"\$\{(?<name>[^}]+)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static object? ResolveObject(object? value, IExecutionContext context)
    {
        return value switch
        {
            null => null,
            string s => ResolveString(s, context),
            IDictionary dictionary => ResolveDictionary(dictionary, context),
            IEnumerable enumerable when value is not string => ResolveEnumerable(enumerable, context),
            _ => value
        };
    }

    private static object ResolveDictionary(IDictionary dictionary, IExecutionContext context)
    {
        var result = new Dictionary<string, object?>();

        foreach (DictionaryEntry item in dictionary)
        {
            var key = Convert.ToString(item.Key)
                      ?? throw new InvalidOperationException("Ключ словаря не может быть null.");

            result[key] = ResolveObject(item.Value, context);
        }

        return result;
    }

    private static object ResolveEnumerable(IEnumerable enumerable, IExecutionContext context)
    {
        return enumerable.Cast<object?>()
            .Select(item => ResolveObject(item, context))
            .ToList();
    }

    private static object? ResolveString(string input, IExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var matches = VariableRegex.Matches(input);
        if (matches.Count == 0)
            return input;

        if (matches.Count == 1 && matches[0].Value == input)
        {
            var pureExpression = matches[0].Groups["name"].Value;
            return ResolveExpressionValue(pureExpression, context);
        }

        var result = VariableRegex.Replace(input, match =>
        {
            var expression = match.Groups["name"].Value;
            var value = ResolveExpressionValue(expression, context);
            return Convert.ToString(value) ?? string.Empty;
        });

        return result;
    }

    private static object? ResolveExpressionValue(string expression, IExecutionContext context)
    {
        if (expression.StartsWith("env:", StringComparison.OrdinalIgnoreCase))
        {
            var envName = expression["env:".Length..];
            return Environment.GetEnvironmentVariable(envName);
        }

        if (expression.StartsWith("steps.", StringComparison.OrdinalIgnoreCase))
        {
            var parts = expression.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var stepId = parts[1];
                return context.GetStepResult(stepId);
            }
        }

        var dotIndex = expression.IndexOf('.');
        if (dotIndex > 0)
        {
            var varName = expression[..dotIndex];
            var propertyPath = expression[(dotIndex + 1)..];
            var obj = context.GetVariable(varName);
            return GetNestedProperty(obj, propertyPath);
        }

        return context.GetVariable(expression);
    }

    private static object? GetNestedProperty(object? obj, string propertyPath)
    {
        if (obj is null)
            return null;

        var parts = propertyPath.Split('.');
        var current = obj;

        foreach (var part in parts)
        {
            if (current is null)
                return null;

            if (current is IDictionary<string, object?> dict)
            {
                if (!dict.TryGetValue(part, out var dictValue))
                    return null;
                current = dictValue;
            }
            else if (current is IDictionary genericDict)
            {
                current = genericDict[part];
            }
            else
            {
                var prop = current.GetType().GetProperty(
                    part,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop is null)
                    return null;

                current = prop.GetValue(current);
            }
        }

        return current;
    }
}
