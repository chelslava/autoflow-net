# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-04-16

### Added
- YAML DSL for workflow definitions
- Plugin-based keyword architecture
- Control flow: `if`, `for_each`, `call`, `group`, `parallel`
- Variables: `${var}`, `${env:NAME}`, `${steps.id.outputs}`, `${secret:NAME}`
- Retry with exponential backoff
- Parallel execution
- Secrets management with masking
- Lifecycle hooks for extensibility
- Error handling: `on_error` / `finally` blocks
- JSON and HTML reports
- Browser automation (Playwright)
- SQLite persistence
- CLI with commands: run, validate, history, stats, clean, list-keywords
- Central Package Management via Directory.Packages.props
- SDK version pinning via global.json
- MIT License
- Bilingual documentation (English + Russian)
- Contributing guidelines
- Code of Conduct
- Issue and PR templates
- Security policy
- GitHub Actions CI/CD with NuGet caching and coverage upload
- Dependabot for dependency updates (NuGet + GitHub Actions)
- CodeQL security analysis workflow
- Configuration via appsettings.json
- XML documentation for public API interfaces
- Hierarchical AGENTS.md knowledge base for AI assistants
- Comprehensive test suite (67+ tests)

### Security
- Path traversal protection for file operations
- SSRF protection (private network access control)
- URL scheme validation (http/https only)
- Secrets masking in logs and reports
