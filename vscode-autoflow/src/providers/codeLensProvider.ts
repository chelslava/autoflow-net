import * as vscode from 'vscode';

export class AutoFlowCodeLensProvider implements vscode.CodeLensProvider {
    private _onDidChangeCodeLenses = new vscode.EventEmitter<void>();
    public readonly onDidChangeCodeLenses = this._onDidChangeCodeLenses.event;

    provideCodeLenses(document: vscode.TextDocument, token: vscode.CancellationToken): vscode.CodeLens[] {
        const lenses: vscode.CodeLens[] = [];
        const text = document.getText();
        
        const stepRegex = /^\s*(-\s*)?step:\s*$/gm;
        let match;
        
        while ((match = stepRegex.exec(text))) {
            const line = document.lineAt(document.positionAt(match.index).line);
            const range = new vscode.Range(line.range.start, line.range.end);
            
            const stepIdMatch = text.slice(match.index).match(/id:\s*(\S+)/);
            const stepId = stepIdMatch ? stepIdMatch[1] : null;
            
            if (stepId) {
                lenses.push(new vscode.CodeLens(range, {
                    title: '$(play) Run from here',
                    command: 'autoflow.runFromStep',
                    tooltip: 'Run workflow starting from this step',
                    arguments: [{ stepId }]
                }));
            }
            
            lenses.push(new vscode.CodeLens(range, {
                title: '$(copy) Copy reference',
                command: 'autoflow.copyStepRef',
                tooltip: 'Copy step reference to clipboard',
                arguments: [{ stepId }]
            }));
        }
        
        const usesRegex = /uses:\s*(\S+)/g;
        while ((match = usesRegex.exec(text))) {
            const line = document.lineAt(document.positionAt(match.index).line);
            const range = new vscode.Range(line.range.start, line.range.end);
            const keyword = match[1];
            
            lenses.push(new vscode.CodeLens(range, {
                title: `$(book) ${keyword}`,
                command: 'autoflow.showKeywordHelp',
                tooltip: `Show documentation for ${keyword}`,
                arguments: [keyword]
            }));
        }
        
        return lenses;
    }

    resolveCodeLens?(codeLens: vscode.CodeLens, token: vscode.CancellationToken): vscode.ProviderResult<vscode.CodeLens> {
        return codeLens;
    }
}
