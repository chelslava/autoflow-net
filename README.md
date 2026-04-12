# AutoFlow.NET Starter

Стартовый каркас проекта AutoFlow.NET.

В архиве:
- solution и структура проектов
- базовые контракты
- AST-модель DSL
- минимальный runtime
- стартовый keyword `log.info`
- xUnit-тесты
- GitHub Actions CI
- пример workflow

## Предполагаемый стек
- .NET 10
- C#
- YamlDotNet
- Microsoft.Extensions.*
- xUnit

## Что дальше
1. Реализовать полноценный YAML parser.
2. Добавить `validate`, `list-keywords`, `describe-keyword`, `doctor`.
3. Реализовать `IfNode`, `ForEachNode`, `CallNode`.
4. Добавить `Files`, `HTTP`, `Browser`, `Excel`, `Table`.
5. Расширить отчётность.

## Запуск после установки .NET 10

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml
```
