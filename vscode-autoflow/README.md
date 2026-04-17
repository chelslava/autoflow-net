# AutoFlow.NET VS Code Extension

[![VS Code](https://img.shields.io/badge/VS%20Code-Extension-007ACC?style=for-the-badge&logo=visual-studio-code)](https://code.visualstudio.com/)
[![AutoFlow](https://img.shields.io/badge/AutoFlow.NET-1.1.0-green?style=for-the-badge)](https://github.com/chelslava/autoflow-net)

**Professional VS Code extension for AutoFlow.NET automation framework.** Full IDE experience with syntax highlighting, IntelliSense, code navigation, debugging support, and CLI integration.

---

## Features

### Core Features

| Feature | Description |
|---------|-------------|
| 🎨 **Syntax Highlighting** | Keywords, variables, control flow, YAML structure |
| 💡 **IntelliSense** | Context-aware completions for keywords, arguments, outputs |
| 📖 **Hover Documentation** | Full docs for all 20+ keywords |
| ⚠️ **Diagnostics** | Real-time validation with quick fixes |
| 📝 **Snippets** | 20+ workflow patterns |

### Code Navigation

| Feature | Shortcut | Description |
|---------|----------|-------------|
| **Go to Definition** | `Ctrl+Click` / `F12` | Jump from `${steps.id}` to step definition |
| **Find References** | `Shift+F12` | Find all usages of variables and step IDs |
| **Document Links** | `Ctrl+Click` | Clickable links in variable references |
| **Workspace Symbols** | `Ctrl+T` | Search workflows, tasks, steps by name |

### Code Editing

| Feature | Description |
|---------|-------------|
| **Code Actions** | Quick fixes for missing fields |
| **Signature Help** | Parameter hints while typing |
| **Code Folding** | Collapse blocks (tasks, steps, parallel, retry) |
| **Smart Completions** | Variables from `variables:` section |
| **Enum Completions** | Dropdown for `method`, `state`, `browser` |

### UI & Visualization

| Feature | Location | Description |
|---------|----------|-------------|
| **Status Bar** | Bottom right | Shows workflow run status |
| **Tree View** | Explorer sidebar | Workflow structure browser |

---

## Installation

### From VSIX

1. Download `autoflow-1.1.0.vsix` from [Releases](https://github.com/chelslava/autoflow-net/releases)
2. `Ctrl+Shift+X` → Extensions
3. Click `...` → **Install from VSIX...**

### From Source

```bash
cd vscode-autoflow
npm install && npm run compile
# Press F5 in VS Code for development
```

---

## Quick Start

### 1. Create Workflow

Type `af-workflow` + `Tab`:

```yaml
schema_version: 1
name: my_workflow

variables:
  api_base: https://api.example.com

tasks:
  main:
    steps:
      - $0
```

### 2. Add HTTP Request

Type `af-http` + `Tab`:

```yaml
- step:
    id: fetch_users
    uses: http.request
    with:
      url: "${api_base}/users"
      method: GET
    save_as:
      body: users_data
```

### 3. Navigate Code

- `Ctrl+Click` on `${users_data}` → go to variable
- `Ctrl+Click` on `${steps.fetch_users.outputs.body}` → go to step
- `Shift+F12` → find all references

### 4. Run Workflow

`Ctrl+Shift+P` → `AutoFlow: Run Workflow` or click ▶️

---

## IntelliSense

### Keyword Completion

```yaml
- step:
    uses: http.|  # Shows: http.request, json.parse
```

### Argument Completion

```yaml
with:
  method: |  # Dropdown: GET, POST, PUT, PATCH, DELETE
```

### Variable Completion

| Context | Suggestions |
|---------|-------------|
| `${` | var, env:, secret:, steps. |
| `${steps.` | Existing step IDs |
| `${steps.id.outputs.` | Available outputs |

---

## Snippets

| Prefix | Description |
|--------|-------------|
| `af-workflow` | Basic workflow template |
| `af-http` | HTTP request |
| `af-browser-login` | Complete login flow |
| `af-parallel` | Parallel execution |
| `af-retry` | Step with retry |
| `af-file-read/write` | File operations |
| ... | 20+ total |

---

## Commands

| Command | Description |
|---------|-------------|
| `AutoFlow: Run Workflow` | Execute workflow |
| `AutoFlow: Validate Workflow` | Check validity |
| `AutoFlow: Show History` | View executions |
| `AutoFlow: List Keywords` | All keywords |
| `AutoFlow: Show Stats` | Statistics |

---

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `autoflow.projectPath` | auto | Path to AutoFlow.Cli |
| `autoflow.outputFormat` | json | Report format (json/html) |
| `autoflow.showOutputOnRun` | true | Show output panel |
| `autoflow.enableStatusBar` | true | Status bar item |

---

## Example

```yaml
schema_version: 1
name: api_test

tasks:
  main:
    steps:
      - step:
          id: fetch
          uses: http.request
          with:
            url: "https://api.example.com/data"
          save_as:
            body: result

      - step:
          id: parse
          uses: json.parse
          with:
            json: "${steps.fetch.outputs.body}"
            path: "items"
          save_as:
            value: items
```

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| CLI not found | Set `autoflow.projectPath` |
| No IntelliSense | `Ctrl+Space`, check file saved |
| Go to Definition not working | Step must have `id:` field |

---

## Support

- **Issues:** [GitHub Issues](https://github.com/chelslava/autoflow-net/issues)
- **License:** MIT
