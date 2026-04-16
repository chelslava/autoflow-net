# Contributing to AutoFlow.NET

Thank you for your interest in contributing to AutoFlow.NET! This document provides guidelines and instructions for contributing.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [How to Contribute](#how-to-contribute)
- [Pull Request Process](#pull-request-process)
- [Coding Standards](#coding-standards)
- [Commit Guidelines](#commit-guidelines)

## Code of Conduct

This project and everyone participating in it is governed by our [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR_USERNAME/autoflow-net.git`
3. Create a branch: `git checkout -b feature/my-feature`

## Development Setup

### Prerequisites

- .NET 10 SDK
- Git
- (Optional) Docker for containerized testing

### Build and Test

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test --filter "FullyQualifiedName!~Browser"

# Run all tests including browser tests (requires Playwright setup)
dotnet test
```

### Running the CLI

```bash
# Run a workflow
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml

# Validate a workflow
dotnet run --project src/AutoFlow.Cli -- validate examples/flow.yaml

# List available keywords
dotnet run --project src/AutoFlow.Cli -- list-keywords
```

## How to Contribute

### Reporting Bugs

1. Check if the bug has already been reported in [Issues](https://github.com/chelslava/autoflow-net/issues)
2. If not, create a new issue using the [Bug Report template](.github/ISSUE_TEMPLATE/bug_report.md)
3. Include as much detail as possible

### Suggesting Features

1. Check existing issues for similar suggestions
2. Create a new issue using the [Feature Request template](.github/ISSUE_TEMPLATE/feature_request.md)
3. Describe the use case and expected behavior

### Contributing Code

1. Find an issue to work on (look for `good first issue` or `help wanted` labels)
2. Comment on the issue to indicate you're working on it
3. Create a feature branch
4. Make your changes
5. Submit a pull request

## Pull Request Process

1. **Update documentation** if you change functionality
2. **Add tests** for new features or bug fixes
3. **Follow coding standards** (see below)
4. **Update CHANGELOG.md** with your changes
5. **Ensure CI passes** - all tests must pass

### PR Checklist

- [ ] Code compiles without warnings
- [ ] All tests pass
- [ ] New code has test coverage
- [ ] Documentation is updated
- [ ] CHANGELOG.md is updated
- [ ] Commit messages follow conventions

## Coding Standards

### C# Style

- Follow the [.editorconfig](.editorconfig) settings
- Use nullable reference types
- Prefer explicit types over `var` when type is not obvious
- Use `ConfigureAwait(false)` in library code
- Add XML documentation for all public members

### Project Structure

```
src/
├── AutoFlow.Abstractions/  # Contracts and interfaces
├── AutoFlow.Parser/        # YAML parsing
├── AutoFlow.Runtime/       # Execution engine
├── AutoFlow.Validation/    # Workflow validation
├── AutoFlow.Reporting/     # Report generators
├── AutoFlow.Database/      # SQLite persistence
└── AutoFlow.Cli/           # Command-line interface

libraries/
├── AutoFlow.Library.Assertions/  # log.info keyword
├── AutoFlow.Library.Files/       # files.* keywords
├── AutoFlow.Library.Http/        # http.*, json.* keywords
└── AutoFlow.Library.Browser/     # browser.* keywords

tests/
└── *.Tests/              # xUnit test projects
```

### Adding a New Keyword

1. Create a new class in the appropriate library project
2. Implement `IKeywordHandler<TArgs>`
3. Add the `[Keyword]` attribute
4. Register in `Program.cs`
5. Add tests
6. Document in README

Example:

```csharp
[Keyword("my.keyword", Category = "MyCategory", Description = "Does something useful")]
public sealed class MyKeyword : IKeywordHandler<MyKeywordArgs>
{
    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        MyKeywordArgs args,
        CancellationToken cancellationToken = default)
    {
        // Implementation
        return Task.FromResult(KeywordResult.Success("Done"));
    }
}
```

## Commit Guidelines

We follow [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation only
- `style:` - Code style changes (formatting, etc.)
- `refactor:` - Code refactoring
- `test:` - Adding or updating tests
- `chore:` - Maintenance tasks

Examples:
```
feat: add browser.screenshot keyword
fix: handle null values in variable resolution
docs: update README with new examples
test: add tests for parallel execution
```

## Questions?

Feel free to open a [Discussion](https://github.com/chelslava/autoflow-net/discussions) or reach out to the maintainers.

Thank you for contributing! 🎉
