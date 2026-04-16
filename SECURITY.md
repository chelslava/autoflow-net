# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 0.1.x   | :white_check_mark: |

## Reporting a Vulnerability

We take the security of AutoFlow.NET seriously. If you have discovered a security vulnerability, we appreciate your help in disclosing it to us in a responsible manner.

### How to Report

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report them via GitHub Security Advisories:

1. Go to the [Security Advisories page](https://github.com/chelslava/autoflow-net/security/advisories)
2. Click "Report a vulnerability"
3. Fill in the details of the vulnerability

You can also email security issues to: [your-email@example.com]

### What to Include

Please include the following information:

- Type of vulnerability (e.g., path traversal, injection, etc.)
- Full paths of source file(s) related to the manifestation of the vulnerability
- Any special configuration required to reproduce the issue
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the vulnerability

### Response Timeline

- **Initial Response**: Within 48 hours
- **Status Update**: Within 7 days
- **Resolution**: Depends on severity, typically within 30 days

### Disclosure Policy

- We will acknowledge your email within 48 hours
- We will send a more detailed response within 7 days indicating the next steps
- We will keep you informed of the progress towards a fix
- We may ask for additional information or guidance
- We will credit you in the security advisory (unless you prefer to remain anonymous)

## Security Features

AutoFlow.NET includes several security features:

- **Path Traversal Protection**: File operations are restricted to allowed directories
- **URL Validation**: HTTP requests validate schemes and can block private networks
- **Secret Masking**: Secrets are automatically masked in logs and reports
- **Input Validation**: Workflow inputs are validated against schemas

## Security Best Practices

When using AutoFlow.NET:

1. **File Operations**: Configure `AllowedBasePath` to restrict file access
2. **HTTP Requests**: Enable `AllowPrivateNetworks: false` (default) in production
3. **Secrets**: Use environment variables or secure secret providers, never hardcode
4. **Workflows**: Validate workflows from untrusted sources before execution
5. **Updates**: Keep AutoFlow.NET updated to the latest version

## Known Security Considerations

- YAML parsing: Uses YamlDotNet library; keep it updated
- Browser automation: Playwright can execute JavaScript; be cautious with untrusted URLs
- Reflection: Some keywords use reflection; ensure trusted keyword assemblies only
