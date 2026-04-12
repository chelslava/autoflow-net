# AutoFlow.NET DSL — Полное описание формата

## Содержание

1. [Обзор](#обзор)
2. [Структура документа](#структура-документа)
3. [Переменные](#переменные)
4. [Задачи (Tasks)](#задачи-tasks)
5. [Шаги (Steps)](#шаги-steps)
6. [Условия (Conditions)](#условия-conditions)
7. [Циклы (ForEach)](#циклы-foreach)
8. [Вызов задач (Call)](#вызов-задач-call)
9. [Группы (Group)](#группы-group)
10. [Выражения переменных](#выражения-переменных)
11. [Обработка ошибок](#обработка-ошибок)
12. [Доступные Keywords](#доступные-keywords)

---

## Обзор

AutoFlow.NET DSL (Domain Specific Language) — декларативный язык описания процессов автоматизации на основе YAML. Позволяет описывать workflows в человекочитаемом формате с поддержкой:

- Переменных и выражений
- Условного выполнения (if/else)
- Циклов (foreach)
- Вызова подзадач (call)
- Группировки шагов
- Retry и Timeout
- Сохранения результатов в переменные

---

## Структура документа

```yaml
schema_version: 1                    # Версия схемы (обязательно)
name: workflow_name                  # Имя workflow (обязательно)

variables:                           # Глобальные переменные (опционально)
  var_name: value

tasks:                               # Задачи (обязательно)
  task_name:
    steps:
      - step: ...
```

### Минимальный пример

```yaml
schema_version: 1
name: hello_world

tasks:
  main:
    steps:
      - step:
          id: greet
          uses: log.info
          with:
            message: "Hello, World!"
```

---

## Переменные

### Глобальные переменные

Определяются в секции `variables` на верхнем уровне документа:

```yaml
schema_version: 1
name: demo

variables:
  app_name: AutoFlow
  api_url: https://api.example.com
  max_retries: 3
  debug_mode: true

tasks:
  main:
    steps:
      - step:
          id: log_start
          uses: log.info
          with:
            message: "Запуск ${app_name}"
```

### Типы значений

```yaml
variables:
  string_var: "строка"              # Строка
  int_var: 42                        # Целое число
  float_var: 3.14                    # Число с плавающей точкой
  bool_var: true                     # Булево значение
  list_var: ["a", "b", "c"]          # Список
  dict_var:                          # Словарь
    key1: value1
    key2: value2
```

### Переменные окружения

Доступ к переменным окружения через `${env:NAME}`:

```yaml
variables:
  db_host: "${env:DATABASE_HOST}"
  api_key: "${env:API_KEY}"

tasks:
  main:
    steps:
      - step:
          id: log_env
          uses: log.info
          with:
            message: "DB Host: ${db_host}"
```

---

## Задачи (Tasks)

Задачи — основная единица организации workflow. Каждая задача содержит список шагов.

### Определение задачи

```yaml
tasks:
  main:                              # Имя задачи
    description: "Описание задачи"   # Опционально
    inputs:                          # Входные параметры (опционально)
      param_name:
        type: string
        required: true
        default: "default_value"
    outputs:                         # Выходные параметры (опционально)
      result:
        type: string
    steps:
      - step: ...
```

### Входные параметры (inputs)

```yaml
tasks:
  helper_task:
    inputs:
      message:
        type: string
        required: false
        default: "Default message"
      count:
        type: integer
        required: true
    steps:
      - step:
          id: log_input
          uses: log.info
          with:
            message: "${message}"
```

### Вызов задачи с параметрами

```yaml
tasks:
  main:
    steps:
      - call:
          id: call_helper
          task: helper_task
          with:
            message: "Hello from main!"
            count: 5

  helper_task:
    inputs:
      message:
        type: string
        default: "Default"
    steps:
      - step:
          id: show_message
          uses: log.info
          with:
            message: "${message}"
```

---

## Шаги (Steps)

Шаг — базовая единица выполнения. Вызывает keyword с параметрами.

### Базовый шаг

```yaml
- step:
    id: unique_step_id              # Уникальный идентификатор (обязательно)
    uses: keyword.name              # Имя keyword (обязательно)
    with:                           # Параметры keyword
      param1: value1
      param2: value2
```

### Полный синтаксис шага

```yaml
- step:
    id: open_browser
    uses: browser.open
    with:
      url: "https://example.com"
      headless: true
    save_as:                        # Сохранение результатов
      result: browser_instance
    when:                           # Условное выполнение
      eq:
        - "${should_open}"
        - "true"
    continue_on_error: false        # Продолжить при ошибке
    timeout: "30s"                  # Таймаут выполнения
    retry:                          # Повторные попытки
      attempts: 3
      delay: "5s"
```

### Параметр `with`

Параметры передаются в keyword. Значения могут быть:
- Литералами (строки, числа, булевы)
- Выражениями переменных `${var}`
- Вложенными структурами

```yaml
- step:
    id: http_request
    uses: http.request
    with:
      method: POST
      url: "${api_url}/users"
      headers:
        Content-Type: application/json
        Authorization: "Bearer ${api_key}"
      body:
        name: "John"
        email: "john@example.com"
```

### Параметр `save_as`

Сохраняет результаты выполнения шага в переменные.

#### Формат 1: Сохранение всего результата

```yaml
- step:
    id: read_file
    uses: files.read
    with:
      path: config.json
    save_as: config_data            # Вся структура результата
```

#### Формат 2: Извлечение отдельных полей

```yaml
- step:
    id: check_file
    uses: files.exists
    with:
      path: README.md
    save_as:
      exists: file_exists           # Извлечь поле 'exists'
```

**Пример с несколькими полями:**

```yaml
- step:
    id: get_user
    uses: http.request
    with:
      url: "${api_url}/users/1"
    save_as:
      status: response_status
      body: response_body
```

### Параметр `when`

Условное выполнение шага. Шаг выполняется только если условие истинно.

```yaml
- step:
    id: send_notification
    uses: http.request
    with:
      url: "${webhook_url}"
    when:
      eq:
        - "${should_notify}"
        - "true"
```

### Параметр `continue_on_error`

Если `true`, workflow продолжит выполнение даже при ошибке на этом шаге.

```yaml
- step:
    id: cleanup
    uses: files.delete
    with:
      path: temp.txt
    continue_on_error: true         # Не прерывать workflow при ошибке удаления
```

### Параметр `timeout`

Ограничение времени выполнения шага.

Форматы:
- `"30s"` — 30 секунд
- `"5m"` — 5 минут
- `"1000ms"` — 1000 миллисекунд
- `"60"` — 60 секунд (по умолчанию)

```yaml
- step:
    id: long_operation
    uses: http.request
    with:
      url: "${slow_api}"
    timeout: "2m"                   # Максимум 2 минуты
```

### Параметр `retry`

Автоматический повтор при неудаче.

```yaml
- step:
    id: unstable_api
    uses: http.request
    with:
      url: "${api_url}/data"
    retry:
      attempts: 3                   # Количество попыток
      delay: "5s"                   # Задержка между попытками
```

---

## Условия (Conditions)

Условия используются в `if` и `when`. Поддерживаются два формата записи.

### Формат 1: С явным оператором

```yaml
condition:
  var: variable_name                # Имя переменной (опционально)
  op: eq                            # Оператор (обязательно)
  value: expected_value             # Значение для сравнения (опционально)
```

### Формат 2: Короткий (рекомендуется)

```yaml
condition:
  eq: [left_value, right_value]     # Оператор: массив из 2 элементов
```

### Доступные операторы

| Оператор | Описание | Пример |
|----------|----------|--------|
| `eq` | Равно | `eq: ["${status}", "success"]` |
| `ne` | Не равно | `ne: ["${status}", "error"]` |
| `gt` | Больше | `gt: ["${count}", 10]` |
| `ge` | Больше или равно | `ge: ["${count}", 10]` |
| `lt` | Меньше | `lt: ["${count}", 100]` |
| `le` | Меньше или равно | `le: ["${count}", 100]` |
| `contains` | Содержит подстроку | `contains: ["${text}", "error"]` |
| `starts_with` | Начинается с | `starts_with: ["${url}", "https"]` |
| `ends_with` | Заканчивается на | `ends_with: ["${filename}", ".json"]` |
| `exists` | Переменная существует | `exists: ["${config}"]` |

### Примеры условий

```yaml
# Проверка равенства
condition:
  eq:
    - "${file_exists}"
    - true

# Проверка числа
condition:
  gt:
    - "${retry_count}"
    - 3

# Проверка строки
condition:
  contains:
    - "${response}"
    - "success"
```

---

## Условное выполнение (If)

Условный блок выполняет разные ветки в зависимости от условия.

### Базовый if

```yaml
- if:
    id: check_condition
    condition:
      eq:
        - "${should_run}"
        - "true"
    then:
      - step:
          id: run_if_true
          uses: log.info
          with:
            message: "Condition is true"
```

### If с else

```yaml
- if:
    id: check_file
    condition:
      eq:
        - "${file_exists}"
        - "True"
    then:
      - step:
          id: process_file
          uses: files.read
          with:
            path: data.json
    else:
      - step:
          id: skip_file
          uses: log.info
          with:
            message: "File not found, skipping"
```

### Вложенные условия

```yaml
- if:
    id: outer_check
    condition:
      eq:
        - "${env}"
        - "production"
    then:
      - if:
          id: inner_check
          condition:
            eq:
              - "${deploy_enabled}"
              - "true"
          then:
            - step:
                id: deploy
                uses: log.info
                with:
                  message: "Deploying to production"
```

---

## Циклы (ForEach)

Цикл выполняет шаги для каждого элемента коллекции.

### Базовый foreach

```yaml
- foreach:
    id: process_items
    items: ["alpha", "beta", "gamma"]
    as: item
    steps:
      - step:
          id: log_item
          uses: log.info
          with:
            message: "Processing ${item}"
```

### Переменные цикла

Внутри цикла доступны:
- `${item}` — текущий элемент (имя задаётся в `as`)
- `${item_index}` — индекс текущего элемента (0-based)

```yaml
- foreach:
    id: process_environments
    items: ["dev", "staging", "prod"]
    as: env
    steps:
      - step:
          id: log_env
          uses: log.info
          with:
            message: "Processing ${env} [index: ${env_index}]"
```

### Итерация по переменной

```yaml
variables:
  servers:
    - server1.example.com
    - server2.example.com
    - server3.example.com

tasks:
  main:
    steps:
      - foreach:
          id: check_servers
          items: "${servers}"       # Переменная со списком
          as: server
          steps:
            - step:
                id: ping_server
                uses: http.request
                with:
                  url: "https://${server}/health"
```

---

## Вызов задач (Call)

Позволяет вызывать другие задачи из текущей задачи.

### Базовый вызов

```yaml
tasks:
  main:
    steps:
      - call:
          id: call_helper
          task: helper_task

  helper_task:
    steps:
      - step:
          id: helper_step
          uses: log.info
          with:
            message: "Helper executed"
```

### Вызов с параметрами

```yaml
tasks:
  main:
    steps:
      - call:
          id: deploy_service
          task: deploy
          with:
            service_name: "api"
            environment: "production"

  deploy:
    inputs:
      service_name:
        type: string
        required: true
      environment:
        type: string
        default: "development"
    steps:
      - step:
          id: log_deploy
          uses: log.info
          with:
            message: "Deploying ${service_name} to ${environment}"
```

### Сохранение результата вызова

```yaml
tasks:
  main:
    steps:
      - call:
          id: get_config
          task: load_config
          save_as: config_data

  load_config:
    steps:
      - step:
          id: read_config
          uses: files.read
          with:
            path: config.json
```

---

## Группы (Group)

Группирует шаги для логической организации.

### Синтаксис

```yaml
- group:
    id: setup_group
    name: "Setup Environment"
    steps:
      - step:
          id: create_dir
          uses: files.write
          with:
            path: temp/setup.txt
            content: "initialized"
      - step:
          id: verify_setup
          uses: log.info
          with:
            message: "Setup complete"
```

### Пример использования

```yaml
tasks:
  main:
    steps:
      - group:
          id: initialization
          name: "Initialize Resources"
          steps:
            - step:
                id: create_dirs
                uses: log.info
                with:
                  message: "Creating directories..."
            - step:
                id: load_config
                uses: log.info
                with:
                  message: "Loading configuration..."

      - group:
          id: processing
          name: "Process Data"
          steps:
            - step:
                id: transform
                uses: log.info
                with:
                  message: "Transforming data..."
            - step:
                id: validate
                uses: log.info
                with:
                  message: "Validating results..."
```

---

## Выражения переменных

### Синтаксис

Все выражения переменных заключаются в `${...}`:

```yaml
message: "Hello, ${username}!"
url: "${api_base}/users/${user_id}"
```

### Типы выражений

#### 1. Простые переменные

```yaml
${variable_name}
```

#### 2. Переменные окружения

```yaml
${env:VARIABLE_NAME}
```

#### 3. Результаты шагов

```yaml
${steps.step_id.outputs.field_name}
```

**Пример:**

```yaml
- step:
    id: get_user
    uses: http.request
    with:
      url: "${api_url}/users/1"

- step:
    id: log_user
    uses: log.info
    with:
      message: "User name: ${steps.get_user.outputs.body.name}"
```

#### 4. Доступ к полям объекта

```yaml
${object.field}
${object.nested.field}
```

**Пример:**

```yaml
- step:
    id: read_config
    uses: files.read
    with:
      path: config.json
    save_as:
      content: config

- step:
    id: log_db_host
    uses: log.info
    with:
      message: "Database: ${config.database.host}"
```

#### 5. Индекс списка

```yaml
${list[0]}
${list[index]}
```

**Пример:**

```yaml
variables:
  servers:
    - server1.example.com
    - server2.example.com

tasks:
  main:
    steps:
      - step:
          id: log_first_server
          uses: log.info
          with:
            message: "First server: ${servers[0]}"
```

### Интерполяция в строках

Переменные автоматически интерполируются в строках:

```yaml
message: "Processing ${count} items for ${app_name}"
path: "${base_dir}/${sub_dir}/file.txt"
url: "${api_host}:${api_port}/api/v${api_version}"
```

---

## Обработка ошибок

### Continue on Error

```yaml
- step:
    id: cleanup_temp
    uses: files.delete
    with:
      path: temp.txt
    continue_on_error: true     # Не прерывать workflow при ошибке
```

### Retry

```yaml
- step:
    id: flaky_api
    uses: http.request
    with:
      url: "${unstable_api}"
    retry:
      attempts: 5
      delay: "10s"
```

### Условная обработка

```yaml
- step:
    id: risky_operation
    uses: some.keyword
    with:
      param: value
    save_as:
      success: operation_result

- if:
    id: check_result
    condition:
      eq:
        - "${operation_result}"
        - "success"
    then:
      - step:
          id: handle_success
          uses: log.info
          with:
            message: "Operation succeeded"
    else:
      - step:
          id: handle_failure
          uses: log.info
          with:
            message: "Operation failed, applying fallback"
```

---

## Доступные Keywords

### log.info

Записывает информационное сообщение в лог.

```yaml
- step:
    id: log_step
    uses: log.info
    with:
      message: "Processing item ${item_name}"
```

**Параметры:**
- `message` (string) — сообщение для записи

**Результат:**
- `message` — записанное сообщение

---

### files.read

Читает содержимое файла.

```yaml
- step:
    id: read_config
    uses: files.read
    with:
      path: config.json
    save_as:
      content: config_data
```

**Параметры:**
- `path` (string) — путь к файлу

**Результат:**
- `content` (string) — содержимое файла
- `path` (string) — путь к файлу

---

### files.write

Записывает строку в файл.

```yaml
- step:
    id: write_output
    uses: files.write
    with:
      path: output.txt
      content: "Hello, World!"
```

**Параметры:**
- `path` (string) — путь к файлу
- `content` (string) — содержимое для записи

**Результат:**
- `path` (string) — путь к файлу
- `bytes_written` (int) — количество записанных байт

---

### files.exists

Проверяет существование файла.

```yaml
- step:
    id: check_config
    uses: files.exists
    with:
      path: config.json
    save_as:
      exists: config_exists

- if:
    id: process_if_exists
    condition:
      eq:
        - "${config_exists}"
        - true
    then:
      - step:
          id: read_config
          uses: files.read
          with:
            path: config.json
```

**Параметры:**
- `path` (string) — путь к файлу

**Результат:**
- `exists` (bool) — true если файл существует
- `path` (string) — путь к файлу

---

### files.delete

Удаляет файл.

```yaml
- step:
    id: cleanup
    uses: files.delete
    with:
      path: temp.txt
```

**Параметры:**
- `path` (string) — путь к файлу

**Результат:**
- `path` (string) — путь к удалённому файлу
- `deleted` (bool) — true если файл был удалён

---

### http.request

Выполняет HTTP-запрос.

```yaml
- step:
    id: get_users
    uses: http.request
    with:
      method: GET
      url: "https://api.example.com/users"
      headers:
        Authorization: "Bearer ${api_token}"
        Content-Type: application/json
    save_as:
      status: response_status
      body: response_body
```

**Параметры:**
- `method` (string) — HTTP метод (GET, POST, PUT, DELETE, PATCH)
- `url` (string) — URL запроса
- `headers` (dict, optional) — заголовки запроса
- `body` (any, optional) — тело запроса (для POST, PUT, PATCH)
- `timeout` (int, optional) — таймаут в секундах

**Результат:**
- `status` (int) — HTTP статус код
- `body` (any) — тело ответа
- `headers` (dict) — заголовки ответа

---

### json.parse

Парсит JSON и извлекает значение по JSONPath.

```yaml
- step:
    id: parse_response
    uses: json.parse
    with:
      json: "${response_body}"
      path: "$.data.users[0].name"
    save_as:
      value: user_name
```

**Параметры:**
- `json` (string) — JSON строка для парсинга
- `path` (string) — JSONPath выражение

**Результат:**
- `value` (any) — извлечённое значение

---

## Полный пример

```yaml
schema_version: 1
name: deployment_pipeline

variables:
  app_name: MyApp
  environments:
    - development
    - staging
    - production
  api_base: https://api.example.com

tasks:
  main:
    steps:
      - step:
          id: log_start
          uses: log.info
          with:
            message: "🚀 Starting deployment for ${app_name}"

      - step:
          id: check_config
          uses: files.exists
          with:
            path: deploy.config
          save_as:
            exists: config_exists

      - if:
          id: config_branch
          condition:
            eq:
              - "${config_exists}"
              - true
          then:
            - step:
                id: read_config
                uses: files.read
                with:
                  path: deploy.config
                save_as:
                  content: deploy_config

            - step:
                id: log_config
                uses: log.info
                with:
                  message: "📄 Config loaded (${deploy_config.length} chars)"

      - foreach:
          id: deploy_environments
          items: "${environments}"
          as: env
          steps:
            - step:
                id: deploy_env
                uses: http.request
                with:
                  method: POST
                  url: "${api_base}/deploy"
                  headers:
                    Content-Type: application/json
                  body:
                    app: "${app_name}"
                    environment: "${env}"
                save_as:
                  status: deploy_status

            - if:
                id: check_deploy
                condition:
                  eq:
                    - "${deploy_status}"
                    - 200
                then:
                  - step:
                      id: log_success
                      uses: log.info
                      with:
                        message: "✅ ${env} deployed successfully"
                else:
                  - step:
                      id: log_failure
                      uses: log.info
                      with:
                        message: "❌ ${env} deployment failed"

      - call:
          id: notify_team
          task: send_notification
          with:
            message: "Deployment complete for ${app_name}"

  send_notification:
    inputs:
      message:
        type: string
        default: "Notification"
    steps:
      - step:
          id: send
          uses: log.info
          with:
            message: "📧 ${message}"
```

---

## Лучшие практики

### 1. Именование

- Используйте осмысленные идентификаторы для шагов
- Следуйте единому стилю именования (snake_case или camelCase)

```yaml
# Хорошо
- step:
    id: load_user_config
    uses: files.read

# Плохо
- step:
    id: step1
    uses: files.read
```

### 2. Организация кода

- Группируйте связанные шаги в группы
- Выносите повторяющуюся логику в отдельные задачи
- Используйте переменные для констант

```yaml
variables:
  api_base: https://api.example.com
  timeout: 30s

tasks:
  main:
    steps:
      - group:
          id: setup
          name: "Setup"
          steps:
            - step: ...

      - group:
          id: process
          name: "Processing"
          steps:
            - step: ...
```

### 3. Обработка ошибок

- Всегда обрабатывайте потенциальные ошибки
- Используйте `retry` для нестабильных операций
- Логируйте важные события

```yaml
- step:
    id: api_call
    uses: http.request
    with:
      url: "${unstable_endpoint}"
    retry:
      attempts: 3
      delay: "5s"
    continue_on_error: false
```

### 4. Условия

- Используйте короткий формат условий для читаемости
- Избегайте глубоких вложенностей

```yaml
# Хорошо
condition:
  eq:
    - "${status}"
    - "success"

# Менее читаемо
condition:
  left: "${status}"
  op: eq
  value: "success"
```

### 5. Переменные

- Давайте переменным описательные имена
- Документируйте назначение переменных комментариями

```yaml
variables:
  # API configuration
  api_host: api.example.com
  api_port: 443
  api_version: "1"
  
  # Deployment settings
  max_retries: 3
  timeout_seconds: 30
```

---

## Ограничения

1. **Максимальная вложенность** — рекомендуется не более 3-4 уровней
2. **Типы данных** — поддерживаются базовые типы (string, int, float, bool, list, dict)
3. **Выражения** — только интерполяция переменных, нет арифметических операций
4. **Рекурсия** — задачи не могут вызывать сами себя (защита от бесконечной рекурсии)

---

## Отладка

### Валидация workflow

```bash
dotnet run --project src/AutoFlow.Cli -- validate workflow.yaml
```

### Просмотр доступных keywords

```bash
dotnet run --project src/AutoFlow.Cli -- list-keywords
```

### Генерация отчёта

```bash
dotnet run --project src/AutoFlow.Cli -- run workflow.yaml --output report.json
```

Отчёт содержит:
- Статус выполнения workflow
- Время выполнения
- Результаты каждого шага
- Логи выполнения

---

## Заключение

AutoFlow.NET DSL предоставляет мощный и гибкий способ описания процессов автоматизации. Декларативный подход делает workflows:
- Легко читаемыми
- Простыми для поддержки
- Переиспользуемыми
- Тестируемыми

Для дополнительной информации обратитесь к примерам в папке `examples/` проекта.
