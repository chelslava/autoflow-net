import * as vscode from 'vscode';

interface KeywordInfo {
    name: string;
    category: string;
    description: string;
    args: { name: string; type: string; required: boolean; description: string; default?: string }[];
    outputs?: string[];
}

const KEYWORD_DOCS: Map<string, KeywordInfo> = new Map([
    ['http.request', {
        name: 'http.request',
        category: 'HTTP',
        description: 'Executes an HTTP request with full control over method, headers, body, and timeout.',
        args: [
            { name: 'url', type: 'string', required: true, description: 'Target URL (http or https only)' },
            { name: 'method', type: 'string', required: false, description: 'HTTP method', default: 'GET' },
            { name: 'headers', type: 'object', required: false, description: 'Request headers as key-value pairs' },
            { name: 'body', type: 'object', required: false, description: 'Request body (for POST/PUT/PATCH)' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Request timeout in milliseconds' },
            { name: 'allowPrivateNetworks', type: 'boolean', required: false, description: 'Allow requests to localhost/private networks', default: 'false' }
        ],
        outputs: ['statusCode', 'statusText', 'headers', 'body', 'isSuccess']
    }],
    ['json.parse', {
        name: 'json.parse',
        category: 'JSON',
        description: 'Parses a JSON string and optionally extracts a value using dot-notation path.',
        args: [
            { name: 'json', type: 'string', required: true, description: 'JSON string to parse' },
            { name: 'path', type: 'string', required: false, description: 'Dot-notation path (e.g., "data.items.0.name")' }
        ],
        outputs: ['value', 'path']
    }],
    ['files.read', {
        name: 'files.read',
        category: 'Files',
        description: 'Reads file contents into a string. Protected against path traversal attacks.',
        args: [
            { name: 'path', type: 'string', required: true, description: 'File path (relative to base)' },
            { name: 'encoding', type: 'string', required: false, description: 'File encoding (utf-8, ascii)', default: 'utf-8' },
            { name: 'basePath', type: 'string', required: false, description: 'Base directory for path resolution' }
        ],
        outputs: ['content', 'path']
    }],
    ['files.write', {
        name: 'files.write',
        category: 'Files',
        description: 'Writes a string to a file. Creates parent directories if needed.',
        args: [
            { name: 'path', type: 'string', required: true, description: 'File path' },
            { name: 'content', type: 'string', required: true, description: 'Content to write' },
            { name: 'append', type: 'boolean', required: false, description: 'Append to existing file', default: 'false' },
            { name: 'encoding', type: 'string', required: false, description: 'File encoding', default: 'utf-8' },
            { name: 'basePath', type: 'string', required: false, description: 'Base directory' }
        ],
        outputs: ['path', 'size']
    }],
    ['files.exists', {
        name: 'files.exists',
        category: 'Files',
        description: 'Checks if a file exists at the specified path.',
        args: [
            { name: 'path', type: 'string', required: true, description: 'File path to check' },
            { name: 'basePath', type: 'string', required: false, description: 'Base directory' }
        ],
        outputs: ['exists', 'path']
    }],
    ['files.delete', {
        name: 'files.delete',
        category: 'Files',
        description: 'Deletes a file at the specified path.',
        args: [
            { name: 'path', type: 'string', required: true, description: 'File path to delete' },
            { name: 'basePath', type: 'string', required: false, description: 'Base directory' }
        ],
        outputs: ['deleted', 'path']
    }],
    ['browser.open', {
        name: 'browser.open',
        category: 'Browser',
        description: 'Opens a browser (Chromium, Firefox, or WebKit) and creates a new page.',
        args: [
            { name: 'browser', type: 'string', required: false, description: 'Browser type: chromium, firefox, webkit', default: 'chromium' },
            { name: 'headless', type: 'boolean', required: false, description: 'Run in headless mode', default: 'true' },
            { name: 'width', type: 'number', required: false, description: 'Viewport width' },
            { name: 'height', type: 'number', required: false, description: 'Viewport height' },
            { name: 'slowMo', type: 'boolean', required: false, description: 'Slow down operations for debugging', default: 'false' }
        ],
        outputs: ['browserId', 'browser', 'headless', 'width', 'height']
    }],
    ['browser.close', {
        name: 'browser.close',
        category: 'Browser',
        description: 'Closes the browser instance and releases resources.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID from browser.open' }
        ],
        outputs: ['browserId', 'closed']
    }],
    ['browser.goto', {
        name: 'browser.goto',
        category: 'Browser',
        description: 'Navigates to the specified URL.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'url', type: 'string', required: true, description: 'URL to navigate to' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Navigation timeout' },
            { name: 'waitUntilLoad', type: 'boolean', required: false, description: 'Wait for load event', default: 'true' }
        ],
        outputs: ['url', 'title', 'statusCode', 'statusText']
    }],
    ['browser.click', {
        name: 'browser.click',
        category: 'Browser',
        description: 'Clicks an element matching the selector.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'selector', type: 'string', required: true, description: 'CSS selector' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Click timeout' },
            { name: 'force', type: 'boolean', required: false, description: 'Force click (skip actionability checks)', default: 'false' },
            { name: 'delayMs', type: 'number', required: false, description: 'Delay between mousedown and mouseup' }
        ],
        outputs: ['selector', 'clicked']
    }],
    ['browser.fill', {
        name: 'browser.fill',
        category: 'Browser',
        description: 'Fills an input field with the specified value.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'selector', type: 'string', required: true, description: 'CSS selector' },
            { name: 'value', type: 'string', required: true, description: 'Value to fill' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Fill timeout' },
            { name: 'clear', type: 'boolean', required: false, description: 'Clear field before filling', default: 'true' },
            { name: 'delayMs', type: 'number', required: false, description: 'Typing delay between characters' }
        ],
        outputs: ['selector', 'value', 'filled']
    }],
    ['browser.wait', {
        name: 'browser.wait',
        category: 'Browser',
        description: 'Waits for an element to reach the specified state.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'selector', type: 'string', required: true, description: 'CSS selector' },
            { name: 'state', type: 'string', required: false, description: 'State: visible, hidden, attached, detached', default: 'visible' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Wait timeout' }
        ],
        outputs: ['selector', 'state', 'found']
    }],
    ['browser.screenshot', {
        name: 'browser.screenshot',
        category: 'Browser',
        description: 'Takes a screenshot of the page or a specific element.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'path', type: 'string', required: true, description: 'Output file path (PNG)' },
            { name: 'fullPage', type: 'boolean', required: false, description: 'Capture full scrollable page', default: 'false' },
            { name: 'selector', type: 'string', required: false, description: 'Element selector to capture only that element' }
        ],
        outputs: ['path', 'size', 'fullPage', 'selector']
    }],
    ['browser.assert_text', {
        name: 'browser.assert_text',
        category: 'Browser',
        description: 'Asserts that the page or element contains/equals expected text.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'selector', type: 'string', required: false, description: 'CSS selector (default: entire page)' },
            { name: 'expected', type: 'string', required: true, description: 'Expected text' },
            { name: 'contains', type: 'boolean', required: false, description: 'Check contains vs exact match', default: 'true' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Assertion timeout' }
        ],
        outputs: ['selector', 'expected', 'actual', 'contains', 'passed']
    }],
    ['browser.assert_visible', {
        name: 'browser.assert_visible',
        category: 'Browser',
        description: 'Asserts that an element is visible on the page.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'selector', type: 'string', required: true, description: 'CSS selector' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Assertion timeout' }
        ],
        outputs: ['selector', 'visible']
    }],
    ['browser.get_text', {
        name: 'browser.get_text',
        category: 'Browser',
        description: 'Extracts text content from an element.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'selector', type: 'string', required: true, description: 'CSS selector' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Timeout' }
        ],
        outputs: ['selector', 'text']
    }],
    ['browser.hover', {
        name: 'browser.hover',
        category: 'Browser',
        description: 'Hovers the mouse over an element.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'selector', type: 'string', required: true, description: 'CSS selector' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Hover timeout' },
            { name: 'force', type: 'boolean', required: false, description: 'Force hover', default: 'false' }
        ],
        outputs: ['selector', 'hovered']
    }],
    ['browser.press', {
        name: 'browser.press',
        category: 'Browser',
        description: 'Presses a keyboard key.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'key', type: 'string', required: true, description: 'Key: Enter, Tab, Escape, ArrowDown, etc.' },
            { name: 'selector', type: 'string', required: false, description: 'Target element (optional)' },
            { name: 'delayMs', type: 'number', required: false, description: 'Delay between keydown and keyup' }
        ],
        outputs: ['key', 'selector', 'pressed']
    }],
    ['browser.evaluate', {
        name: 'browser.evaluate',
        category: 'Browser',
        description: 'Executes JavaScript in the browser context.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'script', type: 'string', required: true, description: 'JavaScript code to execute' },
            { name: 'arg', type: 'any', required: false, description: 'Argument passed to script' }
        ],
        outputs: ['result']
    }],
    ['log.info', {
        name: 'log.info',
        category: 'Logging',
        description: 'Logs an informational message to the execution log.',
        args: [
            { name: 'message', type: 'string', required: true, description: 'Message to log' }
        ],
        outputs: ['message']
    }]
]);

export class AutoFlowHoverProvider implements vscode.HoverProvider {
    provideHover(
        document: vscode.TextDocument,
        position: vscode.Position,
        token: vscode.CancellationToken
    ): vscode.ProviderResult<vscode.Hover> {
        const range = document.getWordRangeAtPosition(position, /[a-zA-Z_.]+/);
        if (!range) {
            return null;
        }

        const word = document.getText(range);
        const keyword = KEYWORD_DOCS.get(word);
        
        if (!keyword) {
            return null;
        }

        const markdown = this.formatKeywordMarkdown(keyword);
        return new vscode.Hover(markdown, range);
    }

    private formatKeywordMarkdown(keyword: KeywordInfo): vscode.MarkdownString {
        const md = new vscode.MarkdownString();
        md.appendMarkdown(`## ${keyword.name}\n\n`);
        md.appendMarkdown(`**${keyword.category}**\n\n`);
        md.appendMarkdown(`${keyword.description}\n\n`);
        
        md.appendMarkdown('### Arguments\n\n');
        md.appendMarkdown('| Name | Type | Required | Description |\n');
        md.appendMarkdown('|------|------|----------|-------------|\n');
        
        for (const arg of keyword.args) {
            const required = arg.required ? '✓' : '';
            const defaultVal = arg.default ? ` (default: \`${arg.default}\`)` : '';
            md.appendMarkdown(`| \`${arg.name}\` | ${arg.type} | ${required} | ${arg.description}${defaultVal} |\n`);
        }

        if (keyword.outputs && keyword.outputs.length > 0) {
            md.appendMarkdown('\n### Outputs\n\n');
            md.appendMarkdown('```yaml\n');
            md.appendMarkdown('save_as:\n');
            for (const output of keyword.outputs) {
                md.appendMarkdown(`  ${output}: var_${output}\n`);
            }
            md.appendMarkdown('```\n');
        }

        md.appendMarkdown('\n### Example\n\n');
        md.appendMarkdown('```yaml\n');
        md.appendMarkdown(`- step:\n`);
        md.appendMarkdown(`    id: my_step\n`);
        md.appendMarkdown(`    uses: ${keyword.name}\n`);
        md.appendMarkdown(`    with:\n`);
        for (const arg of keyword.args.filter(a => a.required)) {
            const val = this.getExampleValue(arg.type);
            md.appendMarkdown(`      ${arg.name}: ${val}\n`);
        }
        md.appendMarkdown('```\n');

        return md;
    }

    private getExampleValue(type: string): string {
        switch (type) {
            case 'string': return '"..."';
            case 'number': return '1000';
            case 'boolean': return 'true';
            case 'object': return '{}';
            default: return '...';
        }
    }
}
