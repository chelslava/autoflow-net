# AutoFlow.NET

Кроссплатформенный фреймворк автоматизации на .NET 10 с DSL-языком описания процессов.

## Возможности

- **YAML DSL** — описание workflows в декларативном стиле
- **Plugin Architecture** — расширяемая система keywords
- **Control Flow** — if/foreach/call/group/parallel конструкции
- **Variables** — `${var}`, `${env:NAME}`, `${steps.id.outputs}`, `${secret:NAME}`
- **Retry & Timeout** — повторные попытки с exponential backoff, таймауты
- **Parallel Execution** — параллельное выполнение независимых шагов
- **Secrets Management** — безопасная работа с секретами, маскирование в логах
- **Lifecycle Hooks** — расширяемая система событий workflow
- **Error Handling** — on_error/finally блоки на уровне task
- **Reports** — JSON и HTML отчёты о выполнении с маскированием секретов
- **Browser Automation** — Playwright-based браузерная автоматизация
- **Database** — SQLite для хранения истории выполнений

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
# JSON отчёт
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml --output report.json

# HTML отчёт (определяется по расширению)
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml --output report.html

# Явное указание формата
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml --output report.txt --format html
```

С указанием Run ID:

```bash
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml --run-id my-run-123
```

### История выполнений

```bash
# Показать последние 20 запусков
dotnet run --project src/AutoFlow.Cli -- history

# Фильтр по имени workflow
dotnet run --project src/AutoFlow.Cli -- history --workflow demo_flow

# Фильтр по статусу
dotnet run --project src/AutoFlow.Cli -- history --status Failed

# Ограничить количество
dotnet run --project src/AutoFlow.Cli -- history --limit 10
```

### Детали выполнения

```bash
dotnet run --project src/AutoFlow.Cli -- show <run-id>
```

### Статистика

```bash
# Статистика за последние 30 дней
dotnet run --project src/AutoFlow.Cli -- stats

# Статистика по конкретному workflow
dotnet run --project src/AutoFlow.Cli -- stats --workflow demo_flow

# Статистика за 7 дней
dotnet run --project src/AutoFlow.Cli -- stats --days 7
```

### Очистка истории

```bash
# Удалить записи старше 30 дней
dotnet run --project src/AutoFlow.Cli -- clean --older-than 30
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
    on_error:
      - step:
          id: notify_error
          uses: log.info
          with:
            message: "❌ Workflow failed"

    finally:
      - step:
          id: cleanup
          uses: log.info
          with:
            message: "🧹 Cleanup completed"

    steps:
      - step:
          id: log_start
          uses: log.info
          with:
            message: "Запуск ${app_name}"

      - parallel:
          id: fetch_data
          max_concurrency: 3
          steps:
            - step:
                id: fetch_users
                uses: http.request
                with:
                  url: "${api_url}/users"
                  method: GET

            - step:
                id: fetch_posts
                uses: http.request
                with:
                  url: "${api_url}/posts"
                  method: GET

      - step:
          id: call_api
          uses: http.request
          with:
            url: "${api_url}/data"
            method: GET
          retry:
            attempts: 5
            type: exponential
            delay: "1s"
            max_delay: "30s"
```

## Параллельное выполнение

```yaml
tasks:
  main:
    steps:
      - parallel:
          id: parallel_fetch
          max_concurrency: 5
          error_mode: continue  # или fail_fast
          steps:
            - step: { id: api1, uses: http.request, with: { url: api1 } }
            - step: { id: api2, uses: http.request, with: { url: api2 } }
```

## Secrets Management

Автоматическое маскирование секретов в логах и отчётах:

```yaml
tasks:
  main:
    inputs:
      api_key:
        type: string
        required: true
        secret: true  # Маскируется в логах

    steps:
      - step:
          id: use_secret
          uses: http.request
          with:
            url: https://api.example.com
            headers:
              Authorization: "Bearer ${secret:MY_API_KEY}"
```

Secrets загружаются из:
- Переменных окружения: `${secret:env:API_KEY}`
- Файлов (Docker/K8s secrets): `${secret:file:/run/secrets/api_key}`

## Lifecycle Hooks

Создайте hook для перехвата событий:

```csharp
public class MyHook : IWorkflowLifecycleHook
{
    public int Order => 10;

    public Task OnWorkflowStartAsync(WorkflowContext ctx)
    {
        Console.WriteLine($"Workflow started: {ctx.WorkflowName}");
        return Task.CompletedTask;
    }

    public Task OnStepEndAsync(StepContext ctx, StepExecutionResult result)
    {
        Console.WriteLine($"Step {ctx.StepId}: {result.Status}");
        return Task.CompletedTask;
    }
}
```

Регистрация в DI:

```csharp
services.AddSingleton<IWorkflowLifecycleHook, MyHook>();
```

## Retry с Exponential Backoff

```yaml
- step:
    id: unstable_api
    uses: http.request
    with:
      url: https://unstable.api.com
    retry:
      attempts: 5
      type: exponential
      delay: "1s"
      max_delay: "1m"
      backoff_multiplier: 2.0
      retry_on:
        - TimeoutException
        - HttpRequestException
```

## Доступные Keywords

### Logging & Files

| Keyword | Описание |
|---------|----------|
| `log.info` | Записывает сообщение в лог |
| `files.read` | Читает содержимое файла |
| `files.write` | Записывает строку в файл |
| `files.exists` | Проверяет существование файла |
| `files.delete` | Удаляет файл |

### HTTP & JSON

| Keyword | Описание |
|---------|----------|
| `http.request` | Выполняет HTTP-запрос |
| `json.parse` | Парсит JSON и извлекает значение |

### Browser Automation

| Keyword | Описание |
|---------|----------|
| `browser.open` | Открывает браузер (Chromium/Firefox/WebKit) |
| `browser.close` | Закрывает браузер |
| `browser.goto` | Навигирует на URL |
| `browser.click` | Кликает по элементу |
| `browser.fill` | Заполняет поле ввода |
| `browser.wait` | Ожидает появление элемента |
| `browser.get_text` | Получает текст элемента |
| `browser.assert_text` | Проверяет текст на странице |
| `browser.assert_visible` | Проверяет видимость элемента |
| `browser.hover` | Наводит курсор на элемент |
| `browser.press` | Нажимает клавиши |
| `browser.evaluate` | Выполняет JavaScript |
| `browser.screenshot` | Делает скриншот страницы |

## Browser Automation Example

```yaml
schema_version: 1
name: browser_test

variables:
  test_url: "https://example.com"

tasks:
  main:
    steps:
      - step:
          id: open
          uses: browser.open
          with:
            browser: chromium
            headless: true
          save_as:
            browserId: browser_id

      - step:
          id: navigate
          uses: browser.goto
          with:
            browserId: "${browser_id}"
            url: "${test_url}"

      - step:
          id: check_title
          uses: browser.assert_text
          with:
            browserId: "${browser_id}"
            selector: "h1"
            expected: "Example Domain"

      - step:
          id: screenshot
          uses: browser.screenshot
          with:
            browserId: "${browser_id}"
            path: "reports/screenshot.png"

      - step:
          id: close
          uses: browser.close
          with:
            browserId: "${browser_id}"
```

## Структура проекта

```
AutoFlow.sln
├── src/
│   ├── AutoFlow.Abstractions/    # Контракты и модели DSL
│   ├── AutoFlow.Parser/          # YAML → AST парсер
│   ├── AutoFlow.Runtime/         # Движок выполнения
│   │   ├── Hooks/                # Lifecycle hooks
│   │   └── Secrets/              # Secret providers
│   ├── AutoFlow.Validation/      # Валидация workflow
│   ├── AutoFlow.Reporting/       # Генерация отчётов
│   ├── AutoFlow.Database/        # SQLite persistence
│   └── AutoFlow.Cli/             # Консольный интерфейс
├── libraries/
│   ├── AutoFlow.Library.Assertions/  # log.info
│   ├── AutoFlow.Library.Files/       # files.*
│   ├── AutoFlow.Library.Http/        # http.*, json.*
│   └── AutoFlow.Library.Browser/     # browser.*
├── tests/
│   ├── AutoFlow.Parser.Tests/
│   ├── AutoFlow.Runtime.Tests/
│   ├── AutoFlow.Validation.Tests/
│   ├── AutoFlow.Reporting.Tests/
│   └── AutoFlow.Database.Tests/
├── examples/
│   ├── flow.yaml
│   ├── advanced_flow.yaml
│   └── advanced_features.yaml
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

## Статус реализации

| Компонент | Статус |
|-----------|--------|
| YAML Parser | ✅ |
| Validation | ✅ |
| Runtime | ✅ |
| Control Flow (if/foreach/call) | ✅ |
| Parallel Execution | ✅ |
| Lifecycle Hooks | ✅ |
| Secrets Management | ✅ |
| Error Handling (on_error/finally) | ✅ |
| Exponential Backoff Retry | ✅ |
| Libraries (files/http/json) | ✅ |
| JSON Report | ✅ |
| HTML Report | ✅ |
| CLI | ✅ |
| Browser Library | ✅ |
| Database (SQLite) | ✅ |
| Workflow Persistence | ✅ |
| Expression Language | ⏳ |

## Лицензия

MIT
