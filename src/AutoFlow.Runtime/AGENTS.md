# AutoFlow.Runtime

Core execution engine for workflow orchestration.

## STRUCTURE

```
AutoFlow.Runtime/
├── RuntimeEngine.cs       # IRuntimeEngine implementation
├── KeywordExecutor.cs     # Handler invocation
├── KeywordRegistry.cs     # Name → handler mapping
├── VariableResolver.cs    # ${var} interpolation
├── SecretResolver.cs      # ${secret:NAME} resolution
├── ExecutionContext.cs    # Step execution state
├── WorkflowHookRunner.cs  # Lifecycle hook coordination
├── Hooks/                 # IWorkflowLifecycleHook implementations
│   ├── LoggingHook.cs
│   └── MetricsHook.cs
└── Secrets/               # ISecretProvider implementations
    ├── EnvSecretProvider.cs
    ├── FileSecretProvider.cs
    └── CompositeSecretProvider.cs
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Modify execution loop | RuntimeEngine.cs |
| Add lifecycle hook | Hooks/ (implement IWorkflowLifecycleHook) |
| Add secret source | Secrets/ (implement ISecretProvider) |
| Change keyword dispatch | KeywordExecutor.cs |
| Variable interpolation | VariableResolver.cs, SecretResolver.cs |

## CONVENTIONS

- Hooks registered via DI: `services.AddSingleton<IWorkflowLifecycleHook, THook>()`
- Hook execution order controlled by `Order` property
- Secret providers use composite pattern (all registered providers checked)
- Variables resolved recursively (supports nested references)

## ANTI-PATTERNS

- Don't modify KeywordRegistry at runtime after registration
- Don't catch exceptions in hooks - let them propagate to engine
