---
name: Bug Report
about: Report a bug to help us improve
title: '[BUG] '
labels: bug
assignees: ''
---

## Describe the Bug

A clear and concise description of what the bug is.

## Steps to Reproduce

1. Go to '...'
2. Run command '...'
3. See error

## Expected Behavior

A clear and concise description of what you expected to happen.

## Actual Behavior

A clear and concise description of what actually happened.

## Environment

- OS: [e.g., Windows 11, Ubuntu 22.04, macOS 14]
- .NET Version: [e.g., 10.0.100]
- AutoFlow.NET Version: [e.g., 0.1.0]

## Sample Workflow

If applicable, provide a minimal workflow YAML that reproduces the issue:

```yaml
schema_version: 1
name: bug_reproduction
tasks:
  main:
    steps:
      - step:
          id: test
          uses: log.info
          with:
            message: "test"
```

## Logs

```
Paste relevant log output here
```

## Additional Context

Add any other context about the problem here.
