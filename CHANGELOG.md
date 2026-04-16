# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Central Package Management via Directory.Packages.props
- SDK version pinning via global.json
- MIT License
- English documentation (README.en.md)
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

### Security
- Path traversal protection for file operations (files.read, files.write, files.exists, files.delete)
- URL scheme validation in http.request (only http/https allowed)
- Private network access control for HTTP requests (SSRF protection)
- Secrets masking in logs and reports

## [0.1.0] - 2025-04-01

### Added
- YAML DSL for workflow definitions
- Plugin-based keyword architecture
- Control flow: if/foreach/call/group/parallel
- Variables: `${var}`, `${env:NAME}`, `${steps.id.outputs}`, `${secret:NAME}`
- Retry with exponential backoff
- Parallel execution
- Secrets management with masking
- Lifecycle hooks
- Error handling: on_error/finally blocks
- JSON and HTML reports
- Browser automation (Playwright)
- SQLite persistence
- CLI with commands: run, validate, history, stats, clean
