import * as vscode from 'vscode';

export interface KeywordDefinition {
    name: string;
    category: string;
    description: string;
    args: ArgDefinition[];
    outputs?: string[];
}

export interface ArgDefinition {
    name: string;
    type: string;
    required: boolean;
    description: string;
    default?: string;
    enum?: string[];
}

export const KEYWORDS: readonly KeywordDefinition[] = [
    {
        name: 'http.request',
        category: 'HTTP',
        description: 'Executes an HTTP request.',
        args: [
            { name: 'url', type: 'string', required: true, description: 'Target URL' },
            { name: 'method', type: 'string', required: false, description: 'HTTP method', default: 'GET', enum: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS'] },
            { name: 'headers', type: 'object', required: false, description: 'Request headers' },
            { name: 'body', type: 'object', required: false, description: 'Request body (for POST/PUT/PATCH)' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Timeout in milliseconds' },
            { name: 'allowPrivateNetworks', type: 'boolean', required: false, description: 'Allow requests to private networks', default: 'false' }
        ],
        outputs: ['statusCode', 'statusText', 'headers', 'body', 'isSuccess']
    },
    {
        name: 'json.parse',
        category: 'JSON',
        description: 'Parses JSON string and extracts value by path.',
        args: [
            { name: 'json', type: 'string', required: true, description: 'JSON string to parse' },
            { name: 'path', type: 'string', required: false, description: 'Dot-notation path to extract' }
        ],
        outputs: ['value', 'path']
    },
    {
        name: 'files.read',
        category: 'Files',
        description: 'Reads file contents into a string.',
        args: [
            { name: 'path', type: 'string', required: true, description: 'File path (relative to base)' },
            { name: 'encoding', type: 'string', required: false, description: 'File encoding', default: 'utf-8', enum: ['utf-8', 'ascii'] },
            { name: 'basePath', type: 'string', required: false, description: 'Base directory for path resolution' }
        ],
        outputs: ['content', 'path']
    },
    {
        name: 'files.write',
        category: 'Files',
        description: 'Writes a string to a file.',
        args: [
            { name: 'path', type: 'string', required: true, description: 'File path' },
            { name: 'content', type: 'string', required: true, description: 'Content to write' },
            { name: 'append', type: 'boolean', required: false, description: 'Append to file', default: 'false' },
            { name: 'encoding', type: 'string', required: false, description: 'File encoding', default: 'utf-8' },
            { name: 'basePath', type: 'string', required: false, description: 'Base directory' }
        ],
        outputs: ['path', 'size']
    },
    {
        name: 'files.exists',
        category: 'Files',
        description: 'Checks if a file exists.',
        args: [
            { name: 'path', type: 'string', required: true, description: 'File path to check' },
            { name: 'basePath', type: 'string', required: false, description: 'Base directory' }
        ],
        outputs: ['exists', 'path']
    },
    {
        name: 'files.delete',
        category: 'Files',
        description: 'Deletes a file.',
        args: [
            { name: 'path', type: 'string', required: true, description: 'File path to delete' },
            { name: 'basePath', type: 'string', required: false, description: 'Base directory' }
        ],
        outputs: ['deleted', 'path']
    },
    {
        name: 'browser.open',
        category: 'Browser',
        description: 'Opens a browser and creates a new page.',
        args: [
            { name: 'browser', type: 'string', required: false, description: 'Browser type', default: 'chromium', enum: ['chromium', 'firefox', 'webkit'] },
            { name: 'headless', type: 'boolean', required: false, description: 'Run in headless mode', default: 'true' },
            { name: 'width', type: 'number', required: false, description: 'Viewport width' },
            { name: 'height', type: 'number', required: false, description: 'Viewport height' },
            { name: 'slowMo', type: 'boolean', required: false, description: 'Slow down operations', default: 'false' }
        ],
        outputs: ['browserId', 'browser', 'headless', 'width', 'height']
    },
    {
        name: 'browser.close',
        category: 'Browser',
        description: 'Closes the browser.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' }
        ],
        outputs: ['browserId', 'closed']
    },
    {
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
    },
    {
        name: 'browser.click',
        category: 'Browser',
        description: 'Clicks an element.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'selector', type: 'string', required: true, description: 'CSS selector' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Click timeout' },
            { name: 'force', type: 'boolean', required: false, description: 'Force click', default: 'false' },
            { name: 'delayMs', type: 'number', required: false, description: 'Delay between mousedown and mouseup' }
        ],
        outputs: ['selector', 'clicked']
    },
    {
        name: 'browser.fill',
        category: 'Browser',
        description: 'Fills an input field.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'selector', type: 'string', required: true, description: 'CSS selector' },
            { name: 'value', type: 'string', required: true, description: 'Value to fill' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Fill timeout' },
            { name: 'clear', type: 'boolean', required: false, description: 'Clear before filling', default: 'true' },
            { name: 'delayMs', type: 'number', required: false, description: 'Typing delay' }
        ],
        outputs: ['selector', 'value', 'filled']
    },
    {
        name: 'browser.wait',
        category: 'Browser',
        description: 'Waits for an element.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'selector', type: 'string', required: true, description: 'CSS selector' },
            { name: 'state', type: 'string', required: false, description: 'State to wait for', default: 'visible', enum: ['visible', 'hidden', 'attached', 'detached'] },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Wait timeout' }
        ],
        outputs: ['selector', 'state', 'found']
    },
    {
        name: 'browser.screenshot',
        category: 'Browser',
        description: 'Takes a screenshot of the page.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'path', type: 'string', required: true, description: 'Output file path' },
            { name: 'fullPage', type: 'boolean', required: false, description: 'Capture full page', default: 'false' },
            { name: 'selector', type: 'string', required: false, description: 'Element selector to capture' }
        ],
        outputs: ['path', 'size', 'fullPage', 'selector']
    },
    {
        name: 'browser.assert_text',
        category: 'Browser',
        description: 'Asserts text on the page.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'selector', type: 'string', required: false, description: 'CSS selector (optional, defaults to body)' },
            { name: 'expected', type: 'string', required: true, description: 'Expected text' },
            { name: 'contains', type: 'boolean', required: false, description: 'Check if contains (vs exact match)', default: 'true' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Assertion timeout' }
        ],
        outputs: ['selector', 'expected', 'actual', 'contains', 'passed']
    },
    {
        name: 'browser.assert_visible',
        category: 'Browser',
        description: 'Asserts element is visible.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'selector', type: 'string', required: true, description: 'CSS selector' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Assertion timeout' }
        ],
        outputs: ['selector', 'visible']
    },
    {
        name: 'browser.get_text',
        category: 'Browser',
        description: 'Gets text from an element.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'selector', type: 'string', required: true, description: 'CSS selector' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Timeout' }
        ],
        outputs: ['selector', 'text']
    },
    {
        name: 'browser.hover',
        category: 'Browser',
        description: 'Hovers over an element.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'selector', type: 'string', required: true, description: 'CSS selector' },
            { name: 'timeoutMs', type: 'number', required: false, description: 'Hover timeout' },
            { name: 'force', type: 'boolean', required: false, description: 'Force hover', default: 'false' }
        ],
        outputs: ['selector', 'hovered']
    },
    {
        name: 'browser.press',
        category: 'Browser',
        description: 'Presses keyboard keys.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'key', type: 'string', required: true, description: 'Key to press (e.g., Enter, Tab, ArrowDown)' },
            { name: 'selector', type: 'string', required: false, description: 'Target selector (optional)' },
            { name: 'delayMs', type: 'number', required: false, description: 'Delay between keydown and keyup' }
        ],
        outputs: ['key', 'selector', 'pressed']
    },
    {
        name: 'browser.evaluate',
        category: 'Browser',
        description: 'Executes JavaScript in the browser.',
        args: [
            { name: 'browserId', type: 'string', required: true, description: 'Browser instance ID' },
            { name: 'script', type: 'string', required: true, description: 'JavaScript to execute' },
            { name: 'arg', type: 'any', required: false, description: 'Argument to pass to script' }
        ],
        outputs: ['result']
    },
    {
        name: 'log.info',
        category: 'Logging',
        description: 'Logs an informational message.',
        args: [
            { name: 'message', type: 'string', required: true, description: 'Message to log' }
        ],
        outputs: ['message']
    }
] as const;

export const KEYWORD_NAMES = KEYWORDS.map(k => k.name);

export const TOP_LEVEL_KEYWORDS = [
    { name: 'schema_version', description: 'Workflow schema version (should be 1)' },
    { name: 'name', description: 'Workflow name' },
    { name: 'variables', description: 'Workflow-level variables' },
    { name: 'tasks', description: 'Task definitions' }
] as const;

export const TASK_KEYWORDS = [
    { name: 'steps', description: 'List of steps to execute' },
    { name: 'on_error', description: 'Error handling steps' },
    { name: 'finally', description: 'Cleanup steps (always runs)' }
] as const;

export const STEP_KEYWORDS = [
    { name: 'step', description: 'Single step definition' },
    { name: 'parallel', description: 'Parallel execution block' },
    { name: 'for_each', description: 'Loop over items' },
    { name: 'if', description: 'Conditional execution' },
    { name: 'call', description: 'Call another task' },
    { name: 'group', description: 'Logical grouping' }
] as const;

export const STEP_PROPS = [
    { name: 'id', description: 'Step identifier (used in ${steps.id.outputs})' },
    { name: 'uses', description: 'Keyword to use (e.g., http.request)' },
    { name: 'with', description: 'Keyword arguments' },
    { name: 'save_as', description: 'Save outputs to variables' },
    { name: 'retry', description: 'Retry configuration' }
] as const;

export const RETRY_PROPS = [
    { name: 'attempts', description: 'Number of retry attempts' },
    { name: 'type', description: 'Retry type', enum: ['fixed', 'exponential'] },
    { name: 'delay', description: 'Initial delay (e.g., "1s")' },
    { name: 'max_delay', description: 'Maximum delay for exponential backoff' }
] as const;

export function getKeyword(name: string): KeywordDefinition | undefined {
    return KEYWORDS.find(k => k.name === name);
}

export function extractVariables(text: string): Set<string> {
    const variables = new Set<string>();
    const pattern = /\$\{([a-zA-Z_][a-zA-Z0-9_]*)\}/g;
    let match;
    while ((match = pattern.exec(text)) !== null) {
        if (!match[1].includes(':') && !match[1].includes('.')) {
            variables.add(match[1]);
        }
    }
    return variables;
}

export function extractStepIds(text: string): Set<string> {
    const ids = new Set<string>();
    const pattern = /^\s*id:\s*([a-zA-Z_][a-zA-Z0-9_]*)/gm;
    let match;
    while ((match = pattern.exec(text)) !== null) {
        ids.add(match[1]);
    }
    return ids;
}

export function extractWorkflowVariables(text: string): Map<string, string> {
    const variables = new Map<string, string>();
    const inVars = text.match(/variables:\s*\n((?:\s+[a-zA-Z_][a-zA-Z0-9_]*:.*\n?)*)/);
    if (inVars) {
        const varPattern = /^\s+([a-zA-Z_][a-zA-Z0-9_]*):\s*(.*)$/gm;
        let match;
        while ((match = varPattern.exec(inVars[1])) !== null) {
            variables.set(match[1], match[2].trim());
        }
    }
    return variables;
}
