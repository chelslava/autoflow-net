# AutoFlow.NET VS Code Extension Changelog

## [1.1.0] - 2025-04-17

### Added

#### Code Navigation
- **Go to Definition** - Ctrl+Click on `${steps.id}` jumps to step definition
- **Find References** - Shift+F12 finds all usages of variables and step IDs
- **Document Links** - Clickable links in `${steps.id.outputs.key}` references

#### Code Editing
- **Code Actions** - Quick fixes for missing schema_version, name, tasks
- **Signature Help** - Parameter hints when editing keyword arguments
- **Code Folding** - Collapse tasks, steps, parallel, retry blocks
- **Smart Completions** - Auto-suggest variables from `variables:` section
- **Step ID Completion** - List of existing step IDs in `${steps.ID}`
- **Output Completion** - Auto-suggest outputs for current keyword
- **Enum Completions** - Dropdown for method, state, browser type, etc.

#### UI & Visualization
- **Status Bar** - Shows workflow run status (success/failed/running)
- **Tree View** - Workflow structure explorer in sidebar
- **Workspace Symbols** - Ctrl+T to search workflows, tasks, steps

#### Performance
- **Debounced Diagnostics** - 300ms delay for validation on edit
- **Shared Keywords Module** - No duplicate keyword definitions

### Improved
- Better error messages with context
- Sorted completions by priority
- Required arguments shown first

## [1.0.0] - 2025-04-17

### Added
- **Syntax Highlighting**
  - Keywords: `http.request`, `json.parse`, `files.*`, `browser.*`, `log.info`
  - Variables: `${var}`, `${env:}`, `${secret:}`, `${steps.id.outputs.}`
  - Control flow: `if`, `for_each`, `parallel`, `call`, `retry`
  - YAML structure highlighting

- **IntelliSense**
  - Keyword completion after `uses:`
  - Argument completion after `with:`
  - Output completion after `save_as:`
  - Variable snippets

- **Hover Documentation**
  - Full documentation for all keywords
  - Required/optional argument listing
  - Output specification
  - Example usage

- **Diagnostics**
  - Missing `schema_version` warning
  - Unknown keyword detection
  - Invalid variable syntax detection
  - YAML best practices checks

- **CLI Integration**
  - Run workflow command
  - Validate workflow command
  - View execution history
  - List available keywords
  - Show statistics

- **Snippets** (20+ snippets)
  - `af-workflow` - Basic workflow template
  - `af-http` - HTTP request
  - `af-browser-*` - Browser automation steps
  - `af-parallel` - Parallel execution
  - `af-retry` - Step with retry
  - `af-browser-login` - Complete login flow

- **Configuration**
  - Auto-detect AutoFlow.Cli project path
  - Custom project path setting
  - Output format selection (JSON/HTML)
  - Show/hide output panel on run
