<p align="center">
  <a href="README.md">English</a> | <strong>Русский</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 10">
  <img src="https://img.shields.io/badge/YAML-DSL-FFB13B?style=for-the-badge&logo=yaml" alt="YAML DSL">
  <img src="https://img.shields.io/badge/Playwright-Browser-2EAD33?style=for-the-badge&logo=playwright" alt="Playwright">
  <img src="https://img.shields.io/badge/VS_Code-Extension-007ACC?style=for-the-badge&logo=visual-studio-code" alt="VS Code Extension">
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="MIT License">
</p>

<h1 align="center">⚡ AutoFlow.NET</h1>

<p align="center">
  <strong>Автоматизируй всё. Пиши меньше кода. Выпускай быстрее.</strong>
</p>

<p align="center">
  Современный кроссплатформенный фреймворк автоматизации с элегантным YAML DSL.<br>
  Создавай рабочие процессы за минуты, а не дни.
</p>

---

## 🎯 Почему AutoFlow.NET?

**Хватит писать шаблонные скрипты автоматизации.** Опиши рабочие процессы в чистом YAML и позволь движку взять на себя сложность.

```yaml
schema_version: 1
name: fetch_and_process

tasks:
  main:
    steps:
      - parallel:
          max_concurrency: 5
          steps:
            - step: { id: users, uses: http.request, with: { url: "${api}/users" } }
            - step: { id: posts, uses: http.request, with: { url: "${api}/posts" } }
            - step: { id: comments, uses: http.request, with: { url: "${api}/comments" } }
```

Готово. **3 параллельных HTTP запроса** с автоматической обработкой ошибок, логированием и отчётами.

---

## ✨ Возможности

| Возможность | Что это даёт |
|-------------|--------------|
| **YAML DSL** | Описывай процессы декларативно — без сложного кода |
| **Параллельное выполнение** | Запускай независимые шаги одновременно — в 5 раз быстрее |
| **Exponential Backoff Retry** | Авто-повтор с умными задержками — устойчивость по умолчанию |
| **Управление секретами** | Безопасно внедряй секреты — авто-маскирование в логах |
| **Lifecycle Hooks** | Перехватывай любые события — полная наблюдаемость |
| **Браузерная автоматизация** | На базе Playwright — тестируй любое веб-приложение |
| **SQLite хранение** | Полная история выполнений — аудит всего |

---

## 🧩 Расширение для VS Code

Установи расширение AutoFlow.NET для лучшего опыта разработки:

[![VS Code](https://img.shields.io/badge/VS%20Code-Установить-007ACC?style=for-the-badge&logo=visual-studio-code)](https://github.com/chelslava/autoflow-net/releases)

**Возможности:**
- 🎨 Подсветка синтаксиса для YAML workflows
- 💡 IntelliSense для keywords, аргументов и переменных
- 📖 Документация при наведении на любой keyword
- 📝 20+ сниппетов для типичных паттернов
- 🚀 Интеграция с CLI (run, validate, history)

```bash
# Скачай из релизов и установи
code --install-extension autoflow-1.1.0.vsix
```

См. [vscode-autoflow/README.md](vscode-autoflow/README.md) для полной документации.

---

## 🚀 Быстрый старт

```bash
# Клонируй и запусти за 30 секунд
git clone https://github.com/chelslava/autoflow-net.git
cd autoflow-net
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml
```

**Требования:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Установка для браузерной автоматизации

Для работы браузерных workflow установи браузеры Playwright:

```bash
# Сначала собери CLI
dotnet build src/AutoFlow.Cli

# Установи браузеры (Chromium, Firefox, WebKit)
pwsh src/AutoFlow.Cli/bin/Debug/net10.0/playwright.ps1 install

# Или только Chromium (быстрее, ~150 MB)
pwsh src/AutoFlow.Cli/bin/Debug/net10.0/playwright.ps1 install chromium
```

**Запуск браузерного примера:**

```bash
dotnet run --project src/AutoFlow.Cli -- run examples/browser_login.yaml
```

---

## 📖 Примеры

### Готовые workflow из каталога `examples/`

| Файл | Что показывает |
|------|----------------|
| `examples/flow.yaml` | Самый простой запуск с `log.info` |
| `examples/file_roundtrip.yaml` | Локальная работа с файлами: `files.write`, `files.exists`, `files.read`, `datetime.now`, `if` |
| `examples/http_json_report.yaml` | HTTP-запрос + разбор JSON + генерация локального отчёта |
| `examples/excel_summary.yaml` | Скачивание Excel и чтение строк через `excel.read` |
| `examples/imports_report.yaml` | Отдельный import-oriented пример с подключением общих задач и переменных |
| `examples/parallel_fetch_report.yaml` | Отдельный focused-пример `parallel` + сборка отчёта |
| `examples/report_cli_demo.yaml` | Пример для генерации JSON/HTML отчётов через CLI |
| `examples/advanced_flow.yaml` | `if`, `foreach`, `call`, чтение файлов |
| `examples/advanced_features.yaml` | `parallel`, `retry`, `on_error`, `finally` |
| `examples/imports/main.yaml` | Импорты workflow и переиспользуемые задачи |
| `examples/browser_login.yaml` | Браузерный login-flow |
| `examples/browser_ecommerce.yaml` | Браузерный e-commerce сценарий |
| `examples/rpa_challenge.yaml` | Полный RPA Challenge на 10 раундов до `Congratulations!` |
| `examples/reframework/main.yaml` | REFramework-структура для RPA Challenge |

**Быстрый запуск новых примеров:**

```bash
dotnet run --project src/AutoFlow.Cli -- run examples/file_roundtrip.yaml
dotnet run --project src/AutoFlow.Cli -- run examples/http_json_report.yaml
dotnet run --project src/AutoFlow.Cli -- run examples/excel_summary.yaml
dotnet run --project src/AutoFlow.Cli -- run examples/imports_report.yaml
dotnet run --project src/AutoFlow.Cli -- run examples/parallel_fetch_report.yaml
```

### Пример генерации JSON/HTML отчётов через CLI

```bash
# JSON-отчёт
dotnet run --project src/AutoFlow.Cli -- run examples/report_cli_demo.yaml --output reports/report_cli_demo.json

# HTML-отчёт
dotnet run --project src/AutoFlow.Cli -- run examples/report_cli_demo.yaml --output reports/report_cli_demo.html
```

### Параллельные API запросы с повторами

```yaml
schema_version: 1
name: data_pipeline

variables:
  api_base: https://api.example.com

tasks:
  main:
    on_error:
      - step: { id: alert, uses: log.info, with: { message: "❌ Ошибка пайплайна!" } }
    
    finally:
      - step: { id: cleanup, uses: log.info, with: { message: "🧹 Очистка завершена" } }
    
    steps:
      # Параллельные запросы — 3 одновременно
      - parallel:
          id: fetch_data
          max_concurrency: 3
          steps:
            - step:
                id: users
                uses: http.request
                with: { url: "${api_base}/users", method: GET }
                save_as: { body: users_data }
            
            - step:
                id: posts
                uses: http.request
                with: { url: "${api_base}/posts", method: GET }
                save_as: { body: posts_data }
      
      # Авто-повтор с exponential backoff
      - step:
          id: unstable_endpoint
          uses: http.request
          with: { url: "${api_base}/flaky", method: GET }
          retry:
            attempts: 5
            type: exponential
            delay: "1s"
            max_delay: "30s"
```

### Браузерная автоматизация

```yaml
schema_version: 1
name: login_test

tasks:
  main:
    steps:
      - step:
          id: open_browser
          uses: browser.open
          with: { browser: chromium, headless: true }
          save_as: { browserId: browser_id }
      
      - step:
          id: navigate
          uses: browser.goto
          with: { browserId: "${browser_id}", url: "https://app.example.com/login" }
      
      - step:
          id: fill_credentials
          uses: browser.fill
          with:
            browserId: "${browser_id}"
            selector: "#email"
            value: "${secret:TEST_USER_EMAIL}"
      
      - step:
          id: submit
          uses: browser.click
          with: { browserId: "${browser_id}", selector: "button[type=submit]" }
      
      - step:
          id: verify
          uses: browser.assert_text
          with: { browserId: "${browser_id}", selector: ".welcome", expected: "Добро пожаловать" }
```

---

## 🔧 CLI команды

```bash
# Запуск workflow
dotnet run --project src/AutoFlow.Cli -- run workflow.yaml

# Генерация HTML отчёта
dotnet run --project src/AutoFlow.Cli -- run workflow.yaml --output report.html

# Валидация перед запуском
dotnet run --project src/AutoFlow.Cli -- validate workflow.yaml

# Просмотр истории выполнений
dotnet run --project src/AutoFlow.Cli -- history --status Failed

# Статистика
dotnet run --project src/AutoFlow.Cli -- stats --days 7

# Список доступных keywords
dotnet run --project src/AutoFlow.Cli -- list-keywords
```

---

## 🧩 Доступные Keywords

### HTTP и данные

| Keyword | Описание |
|---------|----------|
| `http.request` | HTTP/HTTPS запросы с полным контролем |
| `json.parse` | Извлечение значений из JSON |

### Файлы

| Keyword | Описание |
|---------|----------|
| `files.read` | Чтение содержимого файла |
| `files.write` | Запись в файл |
| `files.exists` | Проверка существования файла |
| `files.delete` | Удаление файла |

### Браузер (Playwright)

| Keyword | Описание |
|---------|----------|
| `browser.open` | Запуск Chromium/Firefox/WebKit |
| `browser.goto` | Навигация на URL |
| `browser.click` | Клик по элементу |
| `browser.fill` | Заполнение полей формы |
| `browser.wait` | Ожидание элементов |
| `browser.screenshot` | Скриншоты страниц |
| `browser.assert_text` | Проверка текста на странице |
| `browser.evaluate` | Выполнение JavaScript |

### Управляющие конструкции

| Keyword | Описание |
|---------|----------|
| `if` | Условное выполнение |
| `for_each` | Цикл по элементам |
| `parallel` | Параллельное выполнение |
| `call` | Вызов задач |
| `group` | Логическая группировка |

---

## 🔐 Безопасность

### Защита от Path Traversal
Файловые операции автоматически отклоняют `../` и абсолютные пути вне разрешённых директорий.

### Защита от SSRF
HTTP запросы к `localhost`, `192.168.x.x`, `10.x.x.x` заблокированы по умолчанию. Включи явно через `allowPrivateNetworks: true`.

### Маскирование секретов
Секреты автоматически маскируются в логах и отчётах:

```
[INFO] Вызов API с токеном: ***
```

---

## 🔌 Расширение своими Keywords

```csharp
[Keyword("slack.notify", Category = "Notifications", Description = "Отправить сообщение в Slack")]
public class SlackNotifyKeyword : IKeywordHandler<SlackNotifyArgs>
{
    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        SlackNotifyArgs args,
        CancellationToken ct = default)
    {
        // Твоя логика здесь
        return KeywordResult.Success(new { messageId = "msg_123" });
    }
}
```

Регистрируй и используй:

```yaml
- step:
    id: notify_team
    uses: slack.notify
    with:
      channel: "#deployments"
      message: "Деплой завершён! 🚀"
```

---

## 🗺️ Roadmap

| Возможность | Статус |
|-------------|--------|
| YAML Parser | ✅ |
| Управляющие конструкции (if/foreach/call) | ✅ |
| Параллельное выполнение | ✅ |
| Lifecycle Hooks | ✅ |
| Управление секретами | ✅ |
| Браузерная автоматизация | ✅ |
| SQLite хранение | ✅ |
| Язык выражений | 🚧 В разработке |
| Визуальный редактор workflow | 📋 В планах |
| Облачное выполнение | 📋 В планах |

---

## 🤝 Участие в разработке

Мы рады любому участию! См. [CONTRIBUTING.md](CONTRIBUTING.md) для руководства.

1. Сделай fork репозитория
2. Создай ветку функции (`git checkout -b feature/amazing-feature`)
3.Закоммить изменения (`git commit -m 'feat: add amazing feature'`)
4. Запушь в ветку (`git push origin feature/amazing-feature`)
5. Открой Pull Request

---

## 📄 Лицензия

MIT License — используй для чего угодно, коммерческого или личного.

---

## 💬 Сообщество

- **Issues**: [GitHub Issues](https://github.com/chelslava/autoflow-net/issues)
- **Discussions**: [GitHub Discussions](https://github.com/chelslava/autoflow-net/discussions)

---

<p align="center">
  <strong>Сделано с ❤️ для инженеров автоматизации</strong>
</p>

<p align="center">
  <a href="#-быстрый-старт">Начни за 30 секунд →</a>
</p>
