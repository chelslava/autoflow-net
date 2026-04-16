# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial public release preparation
- Central Package Management via Directory.Packages.props
- SDK version pinning via global.json
- MIT License
- English documentation (README.en.md)
- Contributing guidelines
- Code of Conduct
- Issue and PR templates
- Security policy
- GitHub Actions CI/CD with NuGet caching
- Path traversal protection for file operations
- URL validation for HTTP requests
- Configuration via appsettings.json

### Security
- Fixed path traversal vulnerability in files.* keywords
- Added URL scheme validation in http.request
- Added private network access control for HTTP requests

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
