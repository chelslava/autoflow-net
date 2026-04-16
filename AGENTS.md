# PROJECT KNOWLEDGE BASE

**Generated:** 2026-04-16
**Commit:** be5b449
**Branch:** main

## OVERVIEW

Cross-platform automation framework on .NET 10 with YAML DSL for workflow definitions. Plugin-based keyword architecture with browser automation (Playwright), HTTP client, file operations, and SQLite persistence.

## STRUCTURE

```
autoflow-starter/
├── src/                        # Core libraries
│   ├── AutoFlow.Abstractions/  # Contracts: IKeywordHandler, IRuntimeEngine, IWorkflowParser
│   ├── AutoFlow.Parser/        # YAML → AST parser
│   ├── AutoFlow.Runtime/       # Execution engine, hooks, secret providers
│   ├── AutoFlow.Validation/    # Workflow validation
│   ├── AutoFlow.Reporting/     # JSON/HTML report generators
│   ├── AutoFlow.Database/      # SQLite persistence layer
│   └── AutoFlow.Cli/           # Entry point (top-level statements)
├── libraries/                  # Keyword implementations
│   ├── AutoFlow.Library.Assertions/  # log.info
│   ├── AutoFlow.Library.Files/       # files.read/write/exists/delete
│   ├── AutoFlow.Library.Http/        # http.request, json.parse
│   └── AutoFlow.Library.Browser/     # browser.* (Playwright)
├── tests/                      # xUnit test projects
└── examples/                   # Sample workflow YAML files
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Add new keyword | `libraries/AutoFlow.Library.X/` | Implement IKeywordHandler<TArgs> |
| Modify runtime engine | `src/AutoFlow.Runtime/RuntimeEngine.cs` | Core execution loop |
| Add workflow node type | `src/AutoFlow.Abstractions/` | IWorkflowNode implementations |
| Change YAML parsing | `src/AutoFlow.Parser/` | YamlWorkflowParser |
| Add lifecycle hook | `src/AutoFlow.Runtime/Hooks/` | IWorkflowLifecycleHook |
| Modify CLI commands | `src/AutoFlow.Cli/Program.cs` | System.CommandLine handlers |
| Add secret provider | `src/AutoFlow.Runtime/Secrets/` | ISecretProvider |
| Browser automation | `libraries/AutoFlow.Library.Browser/` | Playwright wrappers |

## CODE MAP

| Symbol | Type | Location | Role |
|--------|------|----------|------|
| IKeywordHandler | interface | Abstractions | Contract for keyword handlers |
| IRuntimeEngine | interface | Abstractions | Workflow execution contract |
| RuntimeEngine | class | Runtime | Core execution engine |
| KeywordRegistry | class | Runtime | Keyword name → handler mapping |
| KeywordExecutor | class | Runtime | Handler invocation logic |
| WorkflowHookRunner | class | Runtime | Lifecycle hook orchestration |
| SecretResolver | class | Runtime | Variable interpolation with secrets |
| IWorkflowParser | interface | Abstractions | YAML parsing contract |
| WorkflowValidator | class | Validation | Schema + keyword validation |
| IExecutionRepository | interface | Database | Persistence contract |

## CONVENTIONS

- **Language**: C# with nullable reference types enabled
- **Framework**: .NET 10 (net10.0) - requires .NET 10 SDK
- **DI**: Microsoft.Extensions.DependencyInjection throughout
- **CLI**: System.CommandLine for command parsing
- **Tests**: xUnit, exclude browser tests from CI (`FullyQualifiedName!~Browser`)
- **Top-level statements**: CLI entry uses C# 9+ pattern (no explicit Program class)

## ANTI-PATTERNS (THIS PROJECT)

- **CI validation swallows errors**: `ci.yml` uses `|| true` in validation loop - hides failures
- **Build artifacts in repo**: `bin/` and `obj/` directories committed (should be in .gitignore)
- **No global.json**: Missing SDK version pinning

## UNIQUE STYLES

- **Keyword registration**: `[Keyword("name", Category="...", Description="...")]` attribute
- **Variable syntax**: `${var}`, `${env:NAME}`, `${steps.id.outputs}`, `${secret:NAME}`
- **Hook registration**: `services.AddSingleton<IWorkflowLifecycleHook, THook>()`
- **Secret providers**: Composite pattern with EnvSecretProvider + FileSecretProvider

## COMMANDS

```bash
# Build
dotnet build

# Run tests (excluding browser tests)
dotnet test --filter "FullyQualifiedName!~Browser"

# Execute workflow
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml

# Validate workflow
dotnet run --project src/AutoFlow.Cli -- validate examples/flow.yaml

# List available keywords
dotnet run --project src/AutoFlow.Cli -- list-keywords

# View execution history
dotnet run --project src/AutoFlow.Cli -- history
```

## NOTES

- README is in Russian - primary documentation language
- .editorconfig: 4-space indentation, LF line endings, UTF-8
- Browser tests excluded from CI (require Playwright setup)
- Secrets masked in logs and reports automatically
- Retry supports exponential backoff with configurable multiplier
