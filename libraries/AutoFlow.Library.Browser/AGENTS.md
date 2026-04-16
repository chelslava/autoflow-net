# AutoFlow.Library.Browser

Playwright-based browser automation keywords.

## KEYWORDS

| Keyword | Purpose |
|---------|---------|
| browser.open | Open browser (Chromium/Firefox/WebKit) |
| browser.close | Close browser instance |
| browser.goto | Navigate to URL |
| browser.click | Click element |
| browser.fill | Fill input field |
| browser.wait | Wait for element |
| browser.get_text | Extract text from element |
| browser.assert_text | Verify page text |
| browser.assert_visible | Check element visibility |
| browser.hover | Hover over element |
| browser.press | Press keyboard keys |
| browser.evaluate | Execute JavaScript |
| browser.screenshot | Capture page screenshot |

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Add browser keyword | Implement IKeywordHandler, register with KeywordAttribute |
| Configure Playwright | Browser initialization in browser.open handler |
| Selectors strategy | locator patterns in click/fill/wait handlers |

## CONVENTIONS

- `browserId` output from `browser.open` → input for all other browser keywords
- `save_as: browserId: browser_id` pattern to capture browser instance ID
- Headless mode default: `headless: true`
- Supports Chromium, Firefox, WebKit via `browser:` parameter

## ANTI-PATTERNS

- Browser tests excluded from CI (require `--filter "FullyQualifiedName!~Browser"`)
- Don't forget `browser.close` - leaked browser instances consume resources
