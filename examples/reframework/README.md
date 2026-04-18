# AutoFlow REFramework Template

Enterprise-grade RPA framework template based on UiPath REFramework patterns.

## Structure

```
reframework/
├── main.yaml              # Entry point - orchestrates all tasks
├── config/
│   └── settings.yaml      # Configuration variables (URLs, timeouts, etc.)
├── lib/
│   ├── browser.yaml       # Reusable browser operations
│   ├── forms.yaml         # Reusable form operations
│   ├── files.yaml         # Reusable file operations
│   └── data.yaml          # Reusable data operations
└── tasks/
    ├── 01_init.yaml           # Initialization
    ├── 02_get_transaction.yaml # Data retrieval
    ├── 03_process_transaction.yaml # Main processing
    ├── 04_end_process.yaml    # Cleanup
    └── 05_error_handler.yaml  # Error handling
```

## REFramework Principles

| Principle | Implementation |
|-----------|----------------|
| **Init** | Separate task for initialization |
| **Get Transaction** | Isolated data retrieval |
| **Process Transaction** | Main business logic |
| **End Process** | Guaranteed cleanup |
| **Error Handling** | Dedicated error handler task |
| **Finally** | Resource cleanup guaranteed |
| **Configuration** | Centralized in config/settings.yaml |
| **Reusability** | Library tasks in lib/ |

## Usage

```bash
# Run the workflow
dotnet run --project src/AutoFlow.Cli -- run examples/reframework/main.yaml

# Validate the workflow
dotnet run --project src/AutoFlow.Cli -- validate examples/reframework/main.yaml

# View dependency graph
dotnet run --project src/AutoFlow.Cli -- graph examples/reframework/main.yaml
```

## Customization

1. **Edit config/settings.yaml** - Change URLs, timeouts, browser settings
2. **Edit tasks/02_get_transaction.yaml** - Modify data source (Excel, API, etc.)
3. **Edit tasks/03_process_transaction.yaml** - Implement your business logic
4. **Add lib/*.yaml** - Create reusable components

## Error Handling

- Automatic screenshots on errors
- Guaranteed browser cleanup via `finally`
- Detailed logging with timestamps
- Error reports saved to `reports/` directory
