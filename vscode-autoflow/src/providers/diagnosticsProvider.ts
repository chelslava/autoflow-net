import * as vscode from 'vscode';
import { KEYWORD_NAMES } from './keywords';

export class AutoFlowDiagnosticsProvider implements vscode.Disposable {
    private readonly collection: vscode.DiagnosticCollection;
    private readonly disposables: vscode.Disposable[] = [];
    private readonly debounceTimers = new Map<string, NodeJS.Timeout>();
    private readonly debounceMs = 300;

    constructor() {
        this.collection = vscode.languages.createDiagnosticCollection('autoflow');
        this.disposables.push(this.collection);

        vscode.workspace.onDidChangeTextDocument(this.onDocumentChange, this, this.disposables);
        vscode.workspace.onDidOpenTextDocument(this.onDocumentOpen, this, this.disposables);
        vscode.workspace.onDidCloseTextDocument(this.onDocumentClose, this, this.disposables);

        for (const doc of vscode.workspace.textDocuments) {
            if (this.isAutoFlowDocument(doc)) {
                this.validateDocument(doc);
            }
        }
    }

    private isAutoFlowDocument(doc: vscode.TextDocument): boolean {
        return doc.languageId === 'autoflow' || 
               doc.fileName.endsWith('.yaml') || 
               doc.fileName.endsWith('.yml');
    }

    private onDocumentOpen(doc: vscode.TextDocument): void {
        if (this.isAutoFlowDocument(doc)) {
            this.validateDocument(doc);
        }
    }

    private onDocumentClose(doc: vscode.TextDocument): void {
        const uri = doc.uri.toString();
        const timer = this.debounceTimers.get(uri);
        if (timer) {
            clearTimeout(timer);
            this.debounceTimers.delete(uri);
        }
    }

    private onDocumentChange(e: vscode.TextDocumentChangeEvent): void {
        if (this.isAutoFlowDocument(e.document)) {
            this.debounceValidation(e.document);
        }
    }

    private debounceValidation(doc: vscode.TextDocument): void {
        const uri = doc.uri.toString();
        const existingTimer = this.debounceTimers.get(uri);
        if (existingTimer) {
            clearTimeout(existingTimer);
        }

        const timer = setTimeout(() => {
            this.validateDocument(doc);
            this.debounceTimers.delete(uri);
        }, this.debounceMs);

        this.debounceTimers.set(uri, timer);
    }

    private validateDocument(doc: vscode.TextDocument): void {
        const diagnostics: vscode.Diagnostic[] = [];
        const text = doc.getText();
        const lines = text.split('\n');

        this.checkSchemaVersion(lines, diagnostics);
        this.checkRequiredFields(lines, diagnostics);
        this.checkKeywordUsage(lines, diagnostics);
        this.checkVariableSyntax(lines, diagnostics);
        this.checkYamlSyntax(lines, diagnostics);
        this.checkMissingRequiredArgs(lines, diagnostics);

        this.collection.set(doc.uri, diagnostics);
    }

    private checkSchemaVersion(lines: string[], diagnostics: vscode.Diagnostic[]): void {
        const hasSchemaVersion = lines.some(line => 
            line.trim().startsWith('schema_version:')
        );

        if (!hasSchemaVersion) {
            diagnostics.push(new vscode.Diagnostic(
                new vscode.Range(0, 0, 0, 0),
                'Missing schema_version. Add "schema_version: 1" at the top of the workflow.',
                vscode.DiagnosticSeverity.Warning
            ));
        }
    }

    private checkRequiredFields(lines: string[], diagnostics: vscode.Diagnostic[]): void {
        const text = lines.join('\n');
        const hasName = /name:\s*\S+/.test(text);
        const hasTasks = /tasks:/.test(text);

        if (!hasName) {
            const lineNum = lines.findIndex(l => l.trim().startsWith('schema_version:'));
            const line = lineNum >= 0 ? lineNum + 1 : 1;
            diagnostics.push(new vscode.Diagnostic(
                new vscode.Range(line, 0, line, 0),
                'Missing workflow name. Add "name: your_workflow_name".',
                vscode.DiagnosticSeverity.Warning
            ));
        }

        if (!hasTasks) {
            const line = lines.length;
            diagnostics.push(new vscode.Diagnostic(
                new vscode.Range(line, 0, line, 0),
                'Missing tasks section. Add "tasks:" with at least one task.',
                vscode.DiagnosticSeverity.Error
            ));
        }
    }

    private checkKeywordUsage(lines: string[], diagnostics: vscode.Diagnostic[]): void {
        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const match = line.match(/uses:\s*([a-zA-Z_.]+)/);
            
            if (match) {
                const keyword = match[1];
                if (!KEYWORD_NAMES.includes(keyword)) {
                    const keywordStart = line.indexOf(keyword);
                    diagnostics.push(new vscode.Diagnostic(
                        new vscode.Range(i, keywordStart, i, keywordStart + keyword.length),
                        `Unknown keyword: "${keyword}". Check keyword name or implement custom handler.`,
                        vscode.DiagnosticSeverity.Error
                    ));
                }
            }
        }
    }

    private checkVariableSyntax(lines: string[], diagnostics: vscode.Diagnostic[]): void {
        const varPattern = /\$\{([^}]+)\}/g;
        
        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            let match;
            
            while ((match = varPattern.exec(line)) !== null) {
                const varContent = match[1];
                const isValid = this.isValidVariable(varContent);
                
                if (!isValid) {
                    const start = match.index + 2;
                    diagnostics.push(new vscode.Diagnostic(
                        new vscode.Range(i, start, i, start + varContent.length),
                        `Invalid variable syntax: "\${${varContent}}". ` +
                        'Valid formats: ${var}, ${env:NAME}, ${secret:NAME}, ${steps.id.outputs.key}',
                        vscode.DiagnosticSeverity.Warning
                    ));
                }
            }
        }
    }

    private isValidVariable(content: string): boolean {
        if (/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(content)) return true;
        if (/^env:[a-zA-Z_][a-zA-Z0-9_]*$/.test(content)) return true;
        if (/^secret:[a-zA-Z_][a-zA-Z0-9_]*$/.test(content)) return true;
        if (/^steps\.[a-zA-Z_][a-zA-Z0-9_]*\.outputs\.[a-zA-Z_][a-zA-Z0-9_]*$/.test(content)) return true;
        return false;
    }

    private checkYamlSyntax(lines: string[], diagnostics: vscode.Diagnostic[]): void {
        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            
            if (line.includes('\t')) {
                const tabPos = line.indexOf('\t');
                diagnostics.push(new vscode.Diagnostic(
                    new vscode.Range(i, tabPos, i, tabPos + 1),
                    'Use spaces instead of tabs for YAML indentation.',
                    vscode.DiagnosticSeverity.Information
                ));
            }

            const trailingSpaceMatch = line.match(/\s+$/);
            if (trailingSpaceMatch && line.trim().length > 0) {
                diagnostics.push(new vscode.Diagnostic(
                    new vscode.Range(i, line.length - trailingSpaceMatch[0].length, i, line.length),
                    'Trailing whitespace.',
                    vscode.DiagnosticSeverity.Hint
                ));
            }
        }
    }

    private checkMissingRequiredArgs(lines: string[], diagnostics: vscode.Diagnostic[]): void {
        const keywordPattern = /uses:\s*([a-zA-Z_.]+)/;
        const idPattern = /^\s*id:\s*(\S+)/;
        
        interface StepInfo { line: number; keyword: string; args: Set<string>; id?: string }
        const steps: StepInfo[] = [];
        let currentStep: StepInfo | null = null;
        let inWith = false;

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const trimmed = line.trim();

            if (keywordPattern.test(line)) {
                if (currentStep) steps.push(currentStep);
                const match = line.match(keywordPattern);
                currentStep = { line: i, keyword: match![1], args: new Set() };
                inWith = false;
            }

            if (idPattern.test(line) && currentStep) {
                const match = line.match(idPattern);
                currentStep.id = match![1];
            }

            if (trimmed.startsWith('with:') && currentStep) {
                inWith = true;
            }

            if (inWith && currentStep && trimmed.includes(':') && !trimmed.startsWith('with:')) {
                const argMatch = trimmed.match(/^([a-zA-Z_][a-zA-Z0-9_]*):/);
                if (argMatch) {
                    currentStep.args.add(argMatch[1]);
                }
            }

            if (trimmed.startsWith('- step:') || trimmed.startsWith('uses:')) {
                inWith = false;
            }
        }

        if (currentStep) steps.push(currentStep);
    }

    dispose(): void {
        this.debounceTimers.forEach(timer => clearTimeout(timer));
        this.debounceTimers.clear();
        this.disposables.forEach(d => d.dispose());
    }
}
