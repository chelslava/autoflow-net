<p align="center">
  <strong>English</strong> | <a href="README.ru.md">Русский</a>
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
  <strong>Automate anything. Write less code. Ship faster.</strong>
</p>

<p align="center">
  A modern, cross-platform automation framework with elegant YAML DSL.<br>
  Build workflows in minutes, not days.
</p>

---

## 🎯 Why AutoFlow.NET?

**Stop writing boilerplate automation scripts.** Define your workflows in clean YAML and let the engine handle the complexity.

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

That's it. **3 parallel HTTP requests** with automatic error handling, logging, and reporting.

---

## ✨ Features that matter

| Feature | What it means for you |
|---------|----------------------|
| **YAML DSL** | Describe workflows declaratively — no complex code |
| **Parallel Execution** | Run independent steps concurrently — 5x faster workflows |
| **Exponential Backoff Retry** | Auto-retry with smart delays — resilient by default |
| **Secrets Management** | Inject secrets safely — auto-masked in logs & reports |
| **Lifecycle Hooks** | Intercept any event — full observability |
| **Browser Automation** | Playwright-powered — test any web app |
| **SQLite Persistence** | Full execution history — audit everything |

---

## 🧩 VS Code Extension

Install the AutoFlow.NET extension for the best development experience:

[![VS Code](https://img.shields.io/badge/VS%20Code-Install-007ACC?style=for-the-badge&logo=visual-studio-code)](https://github.com/chelslava/autoflow-net/releases)

**Features:**

| Feature | Description |
|---------|-------------|
| 🎨 **Syntax Highlighting** | Keywords, variables, control flow |
| 💡 **IntelliSense** | Keywords, arguments, outputs, variables |
| 🔍 **Code Navigation** | Go to Definition, Find References, Workspace Symbols |
| ✏️ **Code Editing** | Quick Fixes, Signature Help, Code Folding |
| 🖥️ **UI** | Status Bar, Tree View, Document Links |
| 📝 **Snippets** | 20+ workflow patterns |
| 🚀 **CLI Integration** | Run, validate, history, stats |

```bash
# Download from releases and install
code --install-extension autoflow-1.1.0.vsix
```

See [vscode-autoflow/README.md](vscode-autoflow/README.md) for full documentation.

---

## 🚀 Quick Start

```bash
# Clone and run in 30 seconds
git clone https://github.com/chelslava/autoflow-net.git
cd autoflow-net
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Browser Automation Setup

For browser automation workflows, install Playwright browsers:

```bash
# Build the CLI first
dotnet build src/AutoFlow.Cli

# Install browsers (Chromium, Firefox, WebKit)
pwsh src/AutoFlow.Cli/bin/Debug/net10.0/playwright.ps1 install

# Or install only Chromium (faster, ~150 MB)
pwsh src/AutoFlow.Cli/bin/Debug/net10.0/playwright.ps1 install chromium
```

**Run browser example:**

```bash
dotnet run --project src/AutoFlow.Cli -- run examples/browser_login.yaml
```

---

## 📖 Real-world Example

### Parallel API Fetch with Retry

```yaml
schema_version: 1
name: data_pipeline

variables:
  api_base: https://api.example.com

tasks:
  main:
    on_error:
      - step: { id: alert, uses: log.info, with: { message: "❌ Pipeline failed!" } }
    
    finally:
      - step: { id: cleanup, uses: log.info, with: { message: "🧹 Cleanup done" } }
    
    steps:
      # Fetch in parallel — 3 concurrent requests
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
      
      # Auto-retry with exponential backoff
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

### Browser Automation

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
          with: { browserId: "${browser_id}", selector: ".welcome", expected: "Welcome" }
```

---

## 🔧 CLI Commands

```bash
# Run a workflow
dotnet run --project src/AutoFlow.Cli -- run workflow.yaml

# Generate HTML report
dotnet run --project src/AutoFlow.Cli -- run workflow.yaml --output report.html

# Validate before running
dotnet run --project src/AutoFlow.Cli -- validate workflow.yaml

# View execution history
dotnet run --project src/AutoFlow.Cli -- history --status Failed

# Get statistics
dotnet run --project src/AutoFlow.Cli -- stats --days 7

# List available keywords
dotnet run --project src/AutoFlow.Cli -- list-keywords
```

---

## 🧩 Available Keywords

### HTTP & Data

| Keyword | Description |
|---------|-------------|
| `http.request` | HTTP/HTTPS requests with full control |
| `json.parse` | Extract values from JSON |

### Files

| Keyword | Description |
|---------|-------------|
| `files.read` | Read file contents |
| `files.write` | Write to files |
| `files.exists` | Check file existence |
| `files.delete` | Delete files |

### Browser (Playwright)

| Keyword | Description |
|---------|-------------|
| `browser.open` | Launch Chromium/Firefox/WebKit |
| `browser.goto` | Navigate to URL |
| `browser.click` | Click elements |
| `browser.fill` | Fill form fields |
| `browser.wait` | Wait for elements |
| `browser.screenshot` | Capture screenshots |
| `browser.assert_text` | Verify page content |
| `browser.evaluate` | Execute JavaScript |

### Control Flow

| Keyword | Description |
|---------|-------------|
| `if` | Conditional execution |
| `for_each` | Loop over items |
| `parallel` | Concurrent execution |
| `call` | Reusable tasks |
| `group` | Logical grouping |

---

## 🔐 Security First

### Path Traversal Protection
File operations automatically reject `../` and absolute paths outside allowed directories.

### SSRF Protection
HTTP requests to `localhost`, `192.168.x.x`, `10.x.x.x` blocked by default. Enable explicitly with `allowPrivateNetworks: true`.

### Secret Masking
Secrets are automatically masked in logs and reports:

```
[INFO] Calling API with token: ***
```

---

## 🔌 Extend with Custom Keywords

```csharp
[Keyword("slack.notify", Category = "Notifications", Description = "Send Slack message")]
public class SlackNotifyKeyword : IKeywordHandler<SlackNotifyArgs>
{
    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        SlackNotifyArgs args,
        CancellationToken ct = default)
    {
        // Your logic here
        return KeywordResult.Success(new { messageId = "msg_123" });
    }
}
```

Register and use:

```yaml
- step:
    id: notify_team
    uses: slack.notify
    with:
      channel: "#deployments"
      message: "Deploy complete! 🚀"
```

---

## 📊 Architecture

```
┌─────────────────────────────────────────────────────────┐
│                     YAML Workflow                        │
└─────────────────────┬───────────────────────────────────┘
                      ▼
┌─────────────────────────────────────────────────────────┐
│                    Parser (AST)                          │
└─────────────────────┬───────────────────────────────────┘
                      ▼
┌─────────────────────────────────────────────────────────┐
│                   Validation                             │
└─────────────────────┬───────────────────────────────────┘
                      ▼
┌─────────────────────────────────────────────────────────┐
│                   Runtime Engine                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │
│  │   Secrets   │  │   Hooks     │  │  Variables  │     │
│  └─────────────┘  └─────────────┘  └─────────────┘     │
└─────────────────────┬───────────────────────────────────┘
                      ▼
┌─────────────────────────────────────────────────────────┐
│              Keyword Executors                           │
│  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐           │
│  │  HTTP  │ │ Files  │ │Browser │ │ Custom │           │
│  └────────┘ └────────┘ └────────┘ └────────┘           │
└─────────────────────────────────────────────────────────┘
```

---

## 📦 Project Structure

```
AutoFlow.NET/
├── src/
│   ├── AutoFlow.Abstractions/    # Core contracts
│   ├── AutoFlow.Runtime/         # Execution engine
│   ├── AutoFlow.Parser/          # YAML → AST
│   ├── AutoFlow.Validation/      # Schema validation
│   ├── AutoFlow.Reporting/       # JSON/HTML reports
│   ├── AutoFlow.Database/        # SQLite persistence
│   └── AutoFlow.Cli/             # Command-line tool
├── libraries/
│   ├── AutoFlow.Library.Http/    # HTTP keywords
│   ├── AutoFlow.Library.Files/   # File keywords
│   └── AutoFlow.Library.Browser/ # Browser keywords
└── tests/                        # Test projects
```

---

## 🗺️ Roadmap

| Feature | Status |
|---------|--------|
| YAML Parser | ✅ |
| Control Flow (if/foreach/call) | ✅ |
| Parallel Execution | ✅ |
| Lifecycle Hooks | ✅ |
| Secrets Management | ✅ |
| Browser Automation | ✅ |
| SQLite Persistence | ✅ |
| Expression Language | 🚧 |
| Visual Workflow Editor | 📋 Planned |
| Cloud Execution | 📋 Planned |

---

## 🤝 Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

1. Fork the repo
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

MIT License — use it for anything, commercial or personal.

---

## 💬 Community

- **Issues**: [GitHub Issues](https://github.com/chelslava/autoflow-net/issues)
- **Discussions**: [GitHub Discussions](https://github.com/chelslava/autoflow-net/discussions)

---

<p align="center">
  <strong>Made with ❤️ for automation engineers</strong>
</p>

<p align="center">
  <a href="#-quick-start">Get started in 30 seconds →</a>
</p>
