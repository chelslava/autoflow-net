# Deployment Guide: AutoFlow VS Code Extension

## Overview

Two distribution channels:
1. **GitHub Releases** - Manual installation from `.vsix`
2. **VS Code Marketplace** - Automatic updates via extension marketplace

---

## Option 1: GitHub Releases (Immediate)

### Step 1: Add Extension to Main Repository

```bash
# Extension is already at: vscode-autoflow/
# Ensure it's tracked in git
cd /mnt/d/Repo/autoflow-starter
git add vscode-autoflow/
git commit -m "feat: add VS Code extension for AutoFlow"
```

### Step 2: Create Release Workflow

Create `.github/workflows/release-extension.yml`:

```yaml
name: Release VS Code Extension

on:
  release:
    types: [published]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'

      - name: Install dependencies
        working-directory: vscode-autoflow
        run: npm install

      - name: Compile
        working-directory: vscode-autoflow
        run: npm run compile

      - name: Package extension
        working-directory: vscode-autoflow
        run: npx @vscode/vsce package --allow-missing-repository

      - name: Upload to Release
        uses: softprops/action-gh-release@v1
        with:
          files: vscode-autoflow/*.vsix
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### Step 3: Create a Release

```bash
# Tag and push
git tag -a v1.0.1 -m "VS Code Extension v1.0.1"
git push origin v1.0.1

# Or create release via GitHub UI
# The workflow will automatically build and attach .vsix
```

### User Installation from Release

```bash
# Download from: https://github.com/chelslava/autoflow-net/releases
# Install in VS Code:
# Extensions → ... → Install from VSIX...
```

---

## Option 2: VS Code Marketplace (Recommended)

### Prerequisites

1. **Microsoft Account** - Create at https://account.microsoft.com
2. **Azure DevOps Organization** - Create at https://dev.azure.com
3. **Personal Access Token (PAT)**:

```bash
# Go to: https://dev.azure.com/chelslava/_usersSettings/tokens
# Create new token with scopes:
# - Marketplace > Manage
# - Marketplace > Publish
```

### Step 1: Update package.json

```json
{
  "name": "autoflow",
  "displayName": "AutoFlow.NET",
  "publisher": "YOUR_PUBLISHER_ID",  // Create at https://marketplace.visualstudio.com/manage
  "description": "...",
  "icon": "images/icon.png",
  "galleryBanner": {
    "color": "#512BD4",
    "theme": "dark"
  },
  "badges": [
    {
      "url": "https://img.shields.io/github/stars/chelslava/autoflow-net?style=flat-square",
      "href": "https://github.com/chelslava/autoflow-net",
      "description": "GitHub stars"
    }
  ]
}
```

### Step 2: Create Publisher

```bash
# Login to marketplace
npx @vscode/vsce login YOUR_PUBLISHER_ID
# Enter your PAT when prompted

# Or via web:
# https://marketplace.visualstudio.com/manage/publishers
```

### Step 3: Publish

```bash
cd vscode-autoflow

# Validate package
npx @vscode/vsce show autoflow

# Publish to marketplace
npx @vscode/vsce publish

# Or publish specific version
npx @vscode/vsce publish 1.0.1
```

### Step 4: Automated Publishing

Create `.github/workflows/publish-extension.yml`:

```yaml
name: Publish Extension

on:
  push:
    tags:
      - 'v*'

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'

      - name: Install dependencies
        working-directory: vscode-autoflow
        run: npm ci

      - name: Publish to Marketplace
        working-directory: vscode-autoflow
        run: npx @vscode/vsce publish -p ${{ secrets.VSCE_PAT }}
        env:
          VSCE_PAT: ${{ secrets.VSCE_PAT }}

      - name: Upload to GitHub Release
        uses: softprops/action-gh-release@v1
        with:
          files: vscode-autoflow/*.vsix
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

**Add secret:**
1. Go to repo Settings → Secrets → Actions
2. Add `VSCE_PAT` with your Personal Access Token

---

## Recommended Structure

```
autoflow-net/
├── .github/
│   └── workflows/
│       ├── ci.yml                    # Existing CI
│       ├── release-extension.yml     # Build on release
│       └── publish-extension.yml     # Publish to marketplace
├── src/
├── libraries/
├── vscode-autoflow/                  # Extension
│   ├── .vscodeignore
│   ├── CHANGELOG.md
│   ├── LICENSE
│   ├── README.md
│   ├── package.json
│   ├── src/
│   ├── syntaxes/
│   └── snippets/
├── README.md
└── README.ru.md
```

---

## Update README Links

Add to main README.md:

```markdown
## VS Code Extension

Install the AutoFlow.NET extension for VS Code:

[![VS Code](https://img.shields.io/badge/VS%20Code-Install-007ACC?style=for-the-badge&logo=visual-studio-code)](https://marketplace.visualstudio.com/items?itemName=autoflow-net.autoflow)

**Features:**
- Syntax highlighting for YAML workflows
- IntelliSense for keywords and arguments
- Hover documentation
- Snippets for common patterns
- CLI integration

[View on Marketplace](https://marketplace.visualstudio.com/items?itemName=autoflow-net.autoflow) | [Documentation](vscode-autoflow/README.md)
```

---

## Publisher ID Options

Choose one:

1. **Use existing publisher** (if you have one)
2. **Create new publisher**:
   - Go to https://marketplace.visualstudio.com/manage/publishers
   - Click "Create Publisher"
   - Use short ID like `chelslava` or `autoflow-net`
   - Display name: "AutoFlow.NET"

---

## Checklist

### GitHub Releases
- [ ] Extension folder committed to repo
- [ ] Release workflow created
- [ ] First release created with .vsix attached
- [ ] README updated with installation instructions

### VS Code Marketplace
- [ ] Microsoft account created
- [ ] Azure DevOps organization created
- [ ] PAT created with Marketplace permissions
- [ ] Publisher ID created
- [ ] Publisher ID added to package.json
- [ ] Gallery banner configured
- [ ] Badges added
- [ ] Published manually first time
- [ ] Automated workflow created
- [ ] Secret added to repository

---

## Quick Commands

```bash
# Local build and test
cd vscode-autoflow
npm install
npm run compile
npm run package

# Install locally for testing
code --install-extension autoflow-1.0.0.vsix

# Publish to marketplace
npx @vscode/vsce publish

# Unpublish (if needed)
npx @vscode/vsce unpublish autoflow@1.0.0
```

---

## Timeline

| Step | Time | Action |
|------|------|--------|
| 1 | 5 min | Commit extension to repo |
| 2 | 10 min | Create release workflow |
| 3 | 5 min | Create first GitHub release |
| 4 | 15 min | Create Microsoft account + PAT |
| 5 | 5 min | Create publisher ID |
| 6 | 10 min | Configure package.json |
| 7 | 5 min | First manual publish |
| 8 | 10 min | Create automated workflow |
| **Total** | ~1 hour | Full marketplace deployment |
