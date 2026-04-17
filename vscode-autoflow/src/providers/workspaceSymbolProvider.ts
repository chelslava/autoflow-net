import * as vscode from 'vscode';

interface WorkflowSymbol {
    name: string;
    type: vscode.SymbolKind;
    range: vscode.Range;
    children: WorkflowSymbol[];
}

export class AutoFlowWorkspaceSymbolProvider implements vscode.WorkspaceSymbolProvider {
    provideWorkspaceSymbols(
        query: string,
        token: vscode.CancellationToken
    ): vscode.ProviderResult<vscode.SymbolInformation[]> {
        const symbols: vscode.SymbolInformation[] = [];
        const lowerQuery = query.toLowerCase();

        for (const doc of vscode.workspace.textDocuments) {
            if (!this.isAutoFlowDocument(doc)) continue;
            
            const docSymbols = this.extractSymbols(doc);
            for (const symbol of docSymbols) {
                if (symbol.name.toLowerCase().includes(lowerQuery)) {
                    symbols.push(new vscode.SymbolInformation(
                        symbol.name,
                        symbol.type,
                        symbol.range,
                        doc.uri
                    ));
                }
            }
        }

        return symbols;
    }

    resolveWorkspaceSymbol(symbol: vscode.SymbolInformation, token: vscode.CancellationToken): vscode.ProviderResult<vscode.SymbolInformation> {
        return symbol;
    }

    private isAutoFlowDocument(doc: vscode.TextDocument): boolean {
        return doc.languageId === 'autoflow' || 
               doc.fileName.endsWith('.yaml') || 
               doc.fileName.endsWith('.yml');
    }

    private extractSymbols(doc: vscode.TextDocument): WorkflowSymbol[] {
        const symbols: WorkflowSymbol[] = [];
        const text = doc.getText();
        const lines = text.split('\n');

        const nameMatch = text.match(/name:\s*(.+)/);
        if (nameMatch) {
            const lineNum = lines.findIndex(l => l.includes(`name: ${nameMatch[1].trim()}`));
            symbols.push({
                name: nameMatch[1].trim(),
                type: vscode.SymbolKind.File,
                range: new vscode.Range(lineNum, 0, lineNum, lines[lineNum].length),
                children: []
            });
        }

        const taskPattern = /^(\s{2})([a-zA-Z_][a-zA-Z0-9_]*):\s*$/;
        let inTasks = false;

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            if (line.trim() === 'tasks:') {
                inTasks = true;
                continue;
            }
            if (inTasks) {
                const match = line.match(taskPattern);
                if (match) {
                    symbols.push({
                        name: match[2],
                        type: vscode.SymbolKind.Function,
                        range: new vscode.Range(i, 0, i, line.length),
                        children: []
                    });
                } else if (line.trim().length > 0 && !line.startsWith('  ') && !line.startsWith('\t')) {
                    inTasks = false;
                }
            }
        }

        const stepPattern = /^\s*id:\s*([a-zA-Z_][a-zA-Z0-9_]*)/;
        for (let i = 0; i < lines.length; i++) {
            const match = lines[i].match(stepPattern);
            if (match) {
                symbols.push({
                    name: match[1],
                    type: vscode.SymbolKind.Event,
                    range: new vscode.Range(i, 0, i, lines[i].length),
                    children: []
                });
            }
        }

        const varPattern = /^(\s{2})([a-zA-Z_][a-zA-Z0-9_]*):\s*(.+)$/;
        let inVariables = false;

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            if (line.trim() === 'variables:') {
                inVariables = true;
                continue;
            }
            if (inVariables) {
                const match = line.match(varPattern);
                if (match) {
                    symbols.push({
                        name: match[2],
                        type: vscode.SymbolKind.Variable,
                        range: new vscode.Range(i, 0, i, line.length),
                        children: []
                    });
                } else if (line.trim().length > 0 && !line.startsWith('  ') && !line.startsWith('\t')) {
                    inVariables = false;
                }
            }
        }

        return symbols;
    }
}
