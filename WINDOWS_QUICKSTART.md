# AutoFlow.NET - Windows Quick Start

## Prerequisites

1. **Install .NET 10 SDK**
   ```powershell
   # Check if installed
   dotnet --version
   
   # Download from: https://dotnet.microsoft.com/download/dotnet/10.0
   ```

2. **Install Playwright browsers** (for browser automation)
   ```powershell
   dotnet tool install --global Microsoft.Playwright.CLI
   playwright install
   ```

3. **Install VS Code** (optional, for extension)
   ```powershell
   winget install Microsoft.VisualStudioCode
   ```

## Quick Start Commands

### Run from PowerShell

```powershell
# Navigate to project
cd D:\Repo\autoflow-starter

# Run basic example
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml

# Run browser example (login)
dotnet run --project src/AutoFlow.Cli -- run examples/browser_login.yaml

# Run e-commerce example
dotnet run --project src/AutoFlow.Cli -- run examples/browser_ecommerce.yaml

# Run RPA Challenge
dotnet run --project src/AutoFlow.Cli -- run examples/rpa_challenge.yaml

# Run REFramework template
dotnet run --project src/AutoFlow.Cli -- run examples/reframework/main.yaml
```

### Run from CMD

```cmd
cd D:\Repo\autoflow-starter

REM Run basic example
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml

REM Run RPA Challenge
dotnet run --project src/AutoFlow.Cli -- run examples/rpa_challenge.yaml
```

### Use Interactive Menu

```powershell
# Run the interactive menu
.\run.bat
```

## CLI Commands Reference

| Command | Description |
|---------|-------------|
| `run <file.yaml>` | Execute a workflow |
| `validate <file.yaml>` | Validate workflow syntax |
| `validate <file.yaml> --dry-run` | Show execution plan |
| `list-keywords` | Show all available keywords |
| `history` | Show execution history |
| `stats` | Show statistics |
| `new <name> --template <type>` | Create new workflow |
| `graph <file.yaml>` | Generate Mermaid diagram |

## JSON Output (for CI/CD)

```powershell
# Get history as JSON
dotnet run --project src/AutoFlow.Cli -- history --output json

# Get stats as JSON
dotnet run --project src/AutoFlow.Cli -- stats --output json

# Get keywords as JSON
dotnet run --project src/AutoFlow.Cli -- list-keywords --output json
```

## VS Code Extension

```powershell
# Open project in VS Code
code .

# Or build and install extension manually
cd vscode-autoflow
npm install
npm run compile
code --install-extension autoflow-1.1.0.vsix
```

## Build Commands

```powershell
# Build project
dotnet build

# Run tests
dotnet test --filter "FullyQualifiedName!~Browser"

# Publish as single executable
dotnet publish src/AutoFlow.Cli -c Release -r win-x64 --self-contained

# Run published version
.\src\AutoFlow.Cli\bin\Release\net10.0\win-x64\publish\AutoFlow.Cli.exe run examples/flow.yaml
```

## Troubleshooting

### Playwright browsers not found
```powershell
playwright install
```

### .NET SDK not found
```powershell
# Install via winget
winget install Microsoft.DotNet.SDK.10
```

### Port already in use
```powershell
# Find and kill process
netstat -ano | findstr :5000
taskkill /PID <pid> /F
```

## Environment Variables

```powershell
# Set log level
$env:LOG_LEVEL = "Debug"

# Set config path
$env:AUTOFLOW_CONFIG = "config/production.yaml"

# Run with environment
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml
```
