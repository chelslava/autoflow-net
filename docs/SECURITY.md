# AutoFlow.NET Security Guide

## Содержание

1. [Обзор](#обзор)
2. [Управление секретами](#управление-секретами)
3. [SSRF защита](#ssrf-защита)
4. [Path Traversal защита](#path-traversal-защита)
5. [Логирование и маскирование](#логирование-и-маскирование)
6. [Рекомендации по безопасности](#рекомендации-по-безопасности)

---

## Обзор

AutoFlow.NET включает встроенную защиту от типичных угроз безопасности при автоматизации. Эта документация описывает, как работают эти механизмы и как их правильно использовать.

---

## Управление секретами

### Синтаксис секретов

Секреты обозначаются специальным выражением `${secret:ref}`:

```yaml
variables:
  api_key: "${secret:api_key}"
  db_password: "${secret:database.password}"
  
tasks:
  main:
    steps:
      - step:
          id: api_call
          uses: http.request
          with:
            url: "${api_url}"
            headers:
              Authorization: "Bearer ${secret:api_token}"
```

### Провайдеры секретов

#### 1. Переменные окружения (EnvSecretProvider)

```yaml
# ${secret:API_KEY} → значение переменной окружения API_KEY
api_key: "${secret:API_KEY}"
```

#### 2. Файлы секретов (FileSecretProvider)

Секреты читаются из JSON-файла:

**secrets.json:**
```json
{
  "database": {
    "host": "localhost",
    "password": "super_secret"
  },
  "api": {
    "key": "sk-12345"
  }
}
```

```yaml
# ${secret:database.password} → "super_secret"
db_pass: "${secret:database.password}"
```

**Рекомендации по безопасности файлов:**
- Храните файлы секретов в защищённых директориях (`/run/secrets`, `/var/run/secrets`)
- Используйте `600` или `600` права доступа к файлам
- Не включайте файлы секретов в git

### Маскирование секретов

Секреты автоматически маскируются в:

- Логах выполнения
- JSON-отчётах
- HTML-отчётах
- Сообщениях об ошибках

```
[INFO] API call with token: ***
[INFO] Connecting to database with password: ***
```

---

## SSRF защита

### По умолчанию

HTTP-запросы к приватным сетям блокируются по умолчанию:

```yaml
- step:
    id: request
    uses: http.request
    with:
      url: "http://localhost:8080/data"  # ❌ Блокируется
```

### Разрешение приватных сетей

```yaml
- step:
    id: internal_request
    uses: http.request
    with:
      url: "http://localhost:8080/data"
      allowPrivateNetworks: true  # ✅ Разрешено
```

### Проверяемые адреса

SSRF защита блокирует:

- `localhost` и домены `.local`
- `10.x.x.x` (Class A private)
- `172.16.x.x` - `172.31.x.x` (Class B private)
- `192.168.x.x` (Class C private)
- `127.x.x.x` (loopback)

---

## Path Traversal защита

### Защита файловых операций

Все файловые операции проверяют путь на path traversal:

```yaml
- step:
    id: read_file
    uses: files.read
    with:
      path: "../etc/passwd"  # ❌ Блокируется (traversal)
```

### Базовая директория

По умолчанию разрешены только пути внутри рабочей директории:

```yaml
- step:
    id: write_file
    uses: files.write
    with:
      path: "./output/data.txt"  # ✅ Разрешено
```

### Разрешённые директории для FileSecretProvider

По умолчанию разрешены:

- `/run/secrets` (Docker secrets)
- `/var/run/secrets` (Kubernetes secrets)
- `./secrets` (локальная директория)

Максимальный размер файла: **64 KB**

---

## Логирование и маскирование

### Автоматическое маскирование

Все секреты, разрешённые через `SecretResolver`, автоматически регистрируются в `SecretMasker`.

### Пример маскирования

```yaml
variables:
  api_token: "${secret:API_TOKEN}"

tasks:
  main:
    steps:
      - step:
          id: http_request
          uses: http.request
          with:
            url: "${api_url}/data"
            headers:
              Authorization: "Bearer ${secret:API_TOKEN}"
```

**Результат логирования:**
```
HTTP GET https://api.example.com/data
Headers: Authorization: Bearer ***
```

### Маскирование в отчётах

```bash
# JSON отчёт
dotnet run --project src/AutoFlow.Cli -- run workflow.yaml --output report.json

# HTML отчёт
dotnet run --project src/AutoFlow.Cli -- run workflow.yaml --output report.html
```

Секреты автоматически маскируются в обоих отчётах.

---

## Рекомендации по безопасности

### 1. Используйте переменные окружения для секретов

```bash
# Лучший способ
export API_KEY="your-secret-key"
dotnet run --project src/AutoFlow.Cli -- run workflow.yaml
```

### 2. Не храните секреты в YAML

```yaml
# ❌ Плохо
variables:
  api_key: "sk-12345"  # Секрет в YAML

# ✅ Хорошо
variables:
  api_key: "${secret:API_KEY}"  # Ссылка на переменную
```

### 3. Ограничьте доступ к файлам секретов

```bash
# Установите права 600
chmod 600 secrets.json
chmod 600 .env

# Используйте .gitignore
echo "secrets.json" >> .gitignore
echo ".env" >> .gitignore
```

### 4. Используйте allowPrivateNetworks с осторожностью

```yaml
# ❌ Плохо - разрешено везде
- step:
    id: request
    uses: http.request
    with:
      url: "http://192.168.1.1/admin"
      allowPrivateNetworks: true

# ✅ Хорошо - только для доверенных endpoints
- step:
    id: request
    uses: http.request
    with:
      url: "http://localhost:8080/api"
      allowPrivateNetworks: true
```

### 5. Проверяйте пути к файлам

```yaml
# ❌ Плохо - может привести к traversal
- step:
    id: read_file
    uses: files.read
    with:
      path: "${user_input}"  # Пользовательский ввод без валидации

# ✅ Хорошо - используйте безопасные пути
- step:
    id: read_file
    uses: files.read
    with:
      path: "./data/${safe_filename}"
```

### 6. Используйте таймауты

```yaml
- step:
    id: http_request
    uses: http.request
    with:
      url: "${api_url}"
      timeout: "30s"  # Ограничение времени выполнения
```

### 7. Мониторинг и логирование

```yaml
tasks:
  main:
    on_error:
      - step:
          id: notify_error
          uses: log.info
          with:
            message: "Workflow failed - check logs"
```

### 8. Регулярно обновляйте зависимости

```bash
# Проверьте уязвимости
dotnet list package --vulnerable
dotnet audit
```

---

## Часто задаваемые вопросы

### Где хранить файл secrets.json?

```bash
# Рекомендуемые места
/run/secrets/secrets.json        # Docker
/var/run/secrets/secrets.json    # Kubernetes
./secrets/secrets.json           # Локальная разработка
```

### Можно ли использовать несколько провайдеров?

Да! Используйте CompositeSecretProvider:

```csharp
services.AddSingleton<ISecretProvider>(sp =>
{
    var envProvider = new EnvSecretProvider();
    var fileProvider = new FileSecretProvider("secrets.json");
    
    return new CompositeSecretProvider(envProvider, fileProvider);
});
```

### Как отключить SSRF защиту?

```yaml
- step:
    id: request
    uses: http.request
    with:
      url: "http://192.168.1.1"
      allowPrivateNetworks: true
```

**Внимание:** Используйте только для доверенных endpoints!

---

## Контакты

Если вы обнаружили уязвимость безопасности в AutoFlow.NET:

1. **Не создавайте public issue**
2. Отправьте детали на security@autoflow.net
3. Мы ответим в течение 48 часов
4. Исправление будет включено в следующем релизе

---

*Последнее обновление: 2026-04-24*
