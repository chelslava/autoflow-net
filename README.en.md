# AutoFlow.NET

Cross-platform automation framework on .NET 10 with YAML DSL for workflow definitions.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![CI](https://github.com/chelslava/autoflow-net/actions/workflows/ci.yml/badge.svg)](https://github.com/chelslava/autoflow-net/actions/workflows/ci.yml)

## Features

- **YAML DSL** — Declarative workflow definitions
- **Plugin Architecture** — Extensible keyword system
- **Control Flow** — if/foreach/call/group/parallel constructs
- **Variables** — `${var}`, `${env:NAME}`, `${steps.id.outputs}`, `${secret:NAME}`
- **Retry & Timeout** — Retry with exponential backoff, configurable timeouts
- **Parallel Execution** — Concurrent execution of independent steps
- **Secrets Management** — Secure secret handling with log masking
- **Lifecycle Hooks** — Extensible workflow event system
- **Error Handling** — on_error/finally blocks at task level
- **Reports** — JSON and HTML execution reports with secret masking
- **Browser Automation** — Playwright-based browser automation
- **Database** — SQLite for execution history persistence

## Installation

Requires .NET 10 SDK.

```bash
git clone https://github.com/chelslava/autoflow-net.git
cd autoflow-net
dotnet restore
dotnet build
```

## CLI Commands

### Execute workflow

```bash
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml
```

With report output:

```bash
# JSON report
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml --output report.json

# HTML report
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml --output report.html
```

### View execution history

```bash
# Show last 20 executions
dotnet run --project src/AutoFlow.Cli -- history

# Filter by workflow name
dotnet run --project src/AutoFlow.Cli -- history --workflow demo_flow

# Filter by status
dotnet run --project src/AutoFlow.Cli -- history --status Failed
```

### Validate workflow

```bash
dotnet run --project src/AutoFlow.Cli -- validate examples/flow.yaml
```

### List available keywords

```bash
dotnet run --project src/AutoFlow.Cli -- list-keywords
```

## Example Workflow

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
            message: "Starting ${app_name}"

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

## Available Keywords

### Logging & Files

| Keyword | Description |
|---------|-------------|
| `log.info` | Log a message |
| `files.read` | Read file contents |
| `files.write` | Write string to file |
| `files.exists` | Check file existence |
| `files.delete` | Delete a file |

### HTTP & JSON

| Keyword | Description |
|---------|-------------|
| `http.request` | Execute HTTP request |
| `json.parse` | Parse JSON and extract value |

### Browser Automation

| Keyword | Description |
|---------|-------------|
| `browser.open` | Open browser (Chromium/Firefox/WebKit) |
| `browser.close` | Close browser instance |
| `browser.goto` | Navigate to URL |
| `browser.click` | Click element |
| `browser.fill` | Fill input field |
| `browser.wait` | Wait for element |
| `browser.get_text` | Get element text |
| `browser.assert_text` | Assert page text |
| `browser.assert_visible` | Check element visibility |
| `browser.screenshot` | Capture page screenshot |

## Creating Custom Keywords

1. Create an arguments class:

```csharp
public class MyKeywordArgs
{
    public string Param { get; set; } = "";
}
```

2. Create a handler:

```csharp
[Keyword("my.keyword", Category = "MyCategory", Description = "Description")]
public class MyKeyword : IKeywordHandler<MyKeywordArgs>
{
    public Task<KeywordResult> ExecuteAsync(
        MyKeywordArgs args,
        KeywordContext context,
        CancellationToken cancellationToken = default)
    {
        // Implementation
        return Task.FromResult(KeywordResult.Success($"Result: {args.Param}"));
    }
}
```

3. Register in CLI:

```csharp
registry.RegisterKeywordsFromAssembly(typeof(MyKeyword).Assembly);
```

## Project Structure

```
AutoFlow.sln
├── src/
│   ├── AutoFlow.Abstractions/    # Contracts and models
│   ├── AutoFlow.Parser/          # YAML → AST parser
│   ├── AutoFlow.Runtime/         # Execution engine
│   ├── AutoFlow.Validation/      # Workflow validation
│   ├── AutoFlow.Reporting/       # Report generators
│   ├── AutoFlow.Database/        # SQLite persistence
│   └── AutoFlow.Cli/             # CLI entry point
├── libraries/
│   ├── AutoFlow.Library.Assertions/
│   ├── AutoFlow.Library.Files/
│   ├── AutoFlow.Library.Http/
│   └── AutoFlow.Library.Browser/
├── tests/
└── examples/
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
