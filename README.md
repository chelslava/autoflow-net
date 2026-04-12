# AutoFlow.NET

Кроссплатформенный фреймворк автоматизации на .NET 10 с DSL-языком описания процессов.

## Возможности

- **YAML DSL** — описание workflows в декларативном стиле
- **Plugin Architecture** — расширяемая система keywords
- **Control Flow** — if/foreach/call/group конструкции
- **Variables** — `${var}`, `${env:NAME}`, `${steps.id.outputs}`
- **Retry & Timeout** — повторные попытки, таймауты, обработка ошибок
- **Reports** — JSON-отчёты о выполнении

## Установка

Требуется .NET 10 SDK.

```bash
git clone https://github.com/chelslava/autoflow-net.git
cd autoflow-net
dotnet restore
dotnet build
```

## CLI Команды

### Выполнение workflow

```bash
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml
```

С сохранением отчёта:

```bash
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml --output report.json
```

### Валидация workflow

```bash
dotnet run --project src/AutoFlow.Cli -- validate examples/flow.yaml
```

### Список keywords

```bash
dotnet run --project src/AutoFlow.Cli -- list-keywords
```

## Пример workflow

```yaml
schema_version: 1
name: demo_flow

variables:
  app_name: AutoFlow
  api_url: https://api.example.com

tasks:
  main:
    steps:
      - step:
          id: log_start
          uses: log.info
          with:
            message: "Запуск ${app_name}"

      - step:
          id: check_config
          uses: files.exists
          with:
            path: config.json
          save_as:
            exists: config_exists

      - if:
          id: check_branch
          condition:
            eq:
              - "${config_exists}"
              - "True"
          then:
            - step:
                id: read_config
                uses: files.read
                with:
                  path: config.json
                save_as:
                  content: config_data

      - foreach:
          id: process_items
          items: ["alpha", "beta", "gamma"]
          as: item
          steps:
            - step:
                id: log_item
                uses: log.info
                with:
                  message: "Обработка ${item} [${item_index}]"

      - call:
          id: call_task
          task: helper_task

  helper_task:
    steps:
      - step:
          id: helper_log
          uses: log.info
          with:
            message: "Helper task executed"
```

## Доступные Keywords

| Keyword | Описание |
|---------|----------|
| `log.info` | Записывает сообщение в лог |
| `files.read` | Читает содержимое файла |
| `files.write` | Записывает строку в файл |
| `files.exists` | Проверяет существование файла |
| `files.delete` | Удаляет файл |
| `http.request` | Выполняет HTTP-запрос |
| `json.parse` | Парсит JSON и извлекает значение |

## Структура проекта

```
AutoFlow.sln
├── src/
│   ├── AutoFlow.Abstractions/    # Контракты и модели DSL
│   ├── AutoFlow.Parser/          # YAML → AST парсер
│   ├── AutoFlow.Runtime/         # Движок выполнения
│   ├── AutoFlow.Validation/      # Валидация workflow
│   ├── AutoFlow.Reporting/       # Генерация отчётов
│   ├── AutoFlow.PluginModel/     # Базовая модель plugin
│   └── AutoFlow.Cli/             # Консольный интерфейс
├── libraries/
│   ├── AutoFlow.Library.Assertions/  # log.info
│   ├── AutoFlow.Library.Files/       # files.*
│   └── AutoFlow.Library.Http/        # http.*, json.*
├── tests/
│   ├── AutoFlow.Parser.Tests/
│   ├── AutoFlow.Runtime.Tests/
│   └── AutoFlow.Validation.Tests/
├── examples/
│   └── flow.yaml
└── doc/
    └── autoflow_mvp_technical_spec.md
```

## Разработка своего Keyword

1. Создайте класс аргументов:

```csharp
public class MyKeywordArgs
{
    public string Param { get; set; } = "";
}
```

2. Создайте handler:

```csharp
[Keyword("my.keyword", Category = "MyCategory", Description = "Описание keyword")]
public class MyKeyword : IKeywordHandler<MyKeywordArgs>
{
    public Task<KeywordResult> ExecuteAsync(MyKeywordArgs args, KeywordContext context)
    {
        // Логика keyword
        return Task.FromResult(KeywordResult.Success($"Результат: {args.Param}"));
    }
}
```

3. Зарегистрируйте в CLI:

```csharp
registry.RegisterKeywordsFromAssembly(typeof(MyKeyword).Assembly);
```

## Статус MVP

| Компонент | Статус |
|-----------|--------|
| YAML Parser | ✅ |
| Validation | ✅ |
| Runtime | ✅ |
| Control Flow (if/foreach/call) | ✅ |
| Libraries (files/http/json) | ✅ |
| JSON Report | ✅ |
| CLI | ✅ |
| Browser Library | ⏳ |
| HTML Report | ⏳ |

## Лицензия

MIT
