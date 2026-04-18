import * as assert from 'assert';
import { KEYWORDS, getKeyword, extractWorkflowVariables, extractStepIds } from '../providers/keywords';

suite('Keywords Test Suite', () => {
    test('KEYWORDS array should not be empty', () => {
        assert.ok(KEYWORDS.length > 0, 'KEYWORDS should have at least one keyword');
    });

    test('All keywords should have required properties', () => {
        for (const kw of KEYWORDS) {
            assert.ok(kw.name, `Keyword should have name`);
            assert.ok(kw.category, `Keyword ${kw.name} should have category`);
            assert.ok(kw.description, `Keyword ${kw.name} should have description`);
            assert.ok(Array.isArray(kw.args), `Keyword ${kw.name} should have args array`);
        }
    });

    test('getKeyword should return keyword by name', () => {
        const httpKeyword = getKeyword('http.request');
        assert.ok(httpKeyword, 'Should find http.request keyword');
        assert.strictEqual(httpKeyword?.name, 'http.request');
    });

    test('getKeyword should return null for unknown keyword', () => {
        const unknown = getKeyword('unknown.keyword');
        assert.strictEqual(unknown, null);
    });

    test('http.request keyword should have expected arguments', () => {
        const httpKeyword = getKeyword('http.request');
        assert.ok(httpKeyword, 'http.request should exist');
        
        const urlArg = httpKeyword.args.find(a => a.name === 'url');
        assert.ok(urlArg, 'http.request should have url argument');
        assert.strictEqual(urlArg?.required, true);
        
        const methodArg = httpKeyword.args.find(a => a.name === 'method');
        assert.ok(methodArg, 'http.request should have method argument');
        assert.ok(methodArg?.enum, 'method should have enum values');
    });

    test('extractWorkflowVariables should find variables', () => {
        const yaml = `
variables:
  api_base: https://api.example.com
  timeout: 30
`;
        const vars = extractWorkflowVariables(yaml);
        assert.ok(vars.has('api_base'), 'Should find api_base variable');
        assert.ok(vars.has('timeout'), 'Should find timeout variable');
        assert.strictEqual(vars.get('api_base'), 'https://api.example.com');
    });

    test('extractWorkflowVariables should return empty map for no variables', () => {
        const yaml = `
tasks:
  main:
    steps: []
`;
        const vars = extractWorkflowVariables(yaml);
        assert.strictEqual(vars.size, 0);
    });

    test('extractStepIds should find step IDs', () => {
        const yaml = `
tasks:
  main:
    steps:
      - step:
          id: fetch_data
          uses: http.request
      - step:
          id: parse_response
          uses: json.parse
`;
        const ids = extractStepIds(yaml);
        assert.ok(ids.includes('fetch_data'), 'Should find fetch_data step');
        assert.ok(ids.includes('parse_response'), 'Should find parse_response step');
    });

    test('extractStepIds should return empty array for no steps', () => {
        const yaml = `
variables:
  api: test
`;
        const ids = extractStepIds(yaml);
        assert.strictEqual(ids.length, 0);
    });

    test('browser keywords should exist', () => {
        const browserOpen = getKeyword('browser.open');
        assert.ok(browserOpen, 'browser.open should exist');
        
        const browserGoto = getKeyword('browser.goto');
        assert.ok(browserGoto, 'browser.goto should exist');
    });

    test('file keywords should exist', () => {
        const fileRead = getKeyword('files.read');
        assert.ok(fileRead, 'files.read should exist');
        
        const fileWrite = getKeyword('files.write');
        assert.ok(fileWrite, 'files.write should exist');
    });
});
