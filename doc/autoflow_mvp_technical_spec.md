# Техническое задание — AutoFlow.NET

---

# 1. Общая концепция проекта

AutoFlow.NET — это кроссплатформенный automation framework нового поколения на базе .NET 10.

Цель — создать:
- execution engine
- DSL язык для описания процессов
- plugin SDK
- CLI
- систему отчётности

Проект не является копией Robot Framework.

---

# 2. Ключевые архитектурные принципы

1. Язык = AST (не YAML)
2. YAML = только внешний формат
3. Runtime изолирован от UI
4. Плагинная архитектура обязательна
5. DSL строго структурный
6. Нет строковых expression language
7. Только JSON-совместимые данные

---

# 3. Технологический стек

- .NET 10
- C#
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- System.Text.Json
- YamlDotNet
- Playwright .NET
- ClosedXML
- xUnit

---

# 4. Архитектура системы

## 4.1 Слои

- AutoFlow.Abstractions
- AutoFlow.Core
- AutoFlow.Dsl (AST)
- AutoFlow.Parser
- AutoFlow.Runtime
- AutoFlow.Validation
- AutoFlow.Reporting
- AutoFlow.PluginModel
- AutoFlow.Manifest
- AutoFlow.Cli

Libraries:
- Browser
- Files
- HTTP
- Excel
- Table
- Assertions

---

# 5. Модель языка (DSL + AST)

## 5.1 Pipeline

YAML → Parser → AST → ExecutionPlan → Runtime

---

## 5.2 Канонические узлы

- WorkflowDocument
- TaskNode
- StepNode
- IfNode
- ForEachNode
- CallNode
- GroupNode

---

## 5.3 Формат DSL

```yaml
schema_version: 1
name: sample_flow

variables:
  url: https://example.com

tasks:
  main:
    steps:
      - step:
          id: open
          uses: browser.open
          with:
            url: ${url}
```

---

# 6. Task модель (НОВОЕ — ОБЯЗАТЕЛЬНО)

Каждая task поддерживает:

## 6.1 inputs

```yaml
inputs:
  url:
    type: string
    required: true
  users:
    type: table
```

## 6.2 outputs

```yaml
outputs:
  token:
    type: string
  users:
    type: table
```

## 6.3 Scope

Внутри task доступны:
- ${inputs.xxx}
- ${steps.stepId.outputs.xxx}

---

# 7. Call (вызов задач)

```yaml
- call:
    id: call_login
    task: login
    inputs:
      url: https://api
    save_as: login_result
```

Результат:

```yaml
${login_result.outputs.token}
```

---

# 8. StepNode (каноническая модель)

```yaml
- step:
    id: step_id
    uses: keyword.name
    with: {}
    timeout: 30s
    retry:
      attempts: 2
    continue_on_error: false
    when: {}
    save_as: result
```

---

# 9. Переменные

Поддержка:
- ${var}
- ${env:NAME}
- ${steps.stepId.outputs.key}

Ограничения:
- только JSON типы
- runtime объекты запрещены

---

# 10. Таблицы (ВАЖНО)

## 10.1 Каноническая модель

```json
{
  "kind": "table",
  "columns": ["Id", "Name"],
  "rows": [
    { "Id": 1, "Name": "Ivan" }
  ],
  "row_count": 1
}
```

## 10.2 Использование

```yaml
- for_each:
    items: ${users.rows}
    as: user
```

## 10.3 Запрещено

- DataTable в DSL

---

# 11. Управляющие конструкции

## If

```yaml
- if:
    condition:
      var: env
      op: eq
      value: prod
```

## ForEach

```yaml
- for_each:
    items: ${list}
    as: item
```

---

# 12. Runtime

## 12.1 ExecutionContext

- variables
- runtime state
- services
- logger

## 12.2 Runtime state

- browser
- excel
- http

---

## 12.3 Политики

- retry
- timeout

---

# 13. Plugin SDK

```csharp
interface IKeywordHandler<TArgs>
```

Атрибут:

```csharp
[Keyword("name")]
```

---

# 14. Библиотеки

## Logging
## Files
## HTTP
## Browser
## Excel
## Table

---

# 15. CLI

Команды:
- run
- validate
- list-keywords
- describe-keyword
- doctor

---

# 16. Reporting

## JSON
- schemaVersion
- steps
- logs

## HTML
- дерево

---

# 17. Конфигурация

TOML:

```toml
name = "project"
```

---

# 18. Тестирование

- unit
- integration
- negative

---

# 19. CI

GitHub Actions:
- build
- test

---

# 20. Этапы разработки

0. repo + issues + ci
1. solution
2. core
3. parser
4. runtime
5. libs
6. reporting
7. cli

---

# 21. Критерии готовности

- YAML выполняется
- есть browser
- есть report

---

# 22. Ограничения

- нет UI
- нет orchestrator
- нет AI

---

# 23. Итог

Реализовать DSL-driven automation engine с поддержкой аргументов, таблиц, plugin SDK и CLI.

