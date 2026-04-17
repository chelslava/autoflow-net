import * as vscode from 'vscode';
import { extractStepIds } from './keywords';

export class AutoFlowDocumentLinkProvider implements vscode.DocumentLinkProvider {
    provideDocumentLinks(
        document: vscode.TextDocument,
        token: vscode.CancellationToken
    ): vscode.ProviderResult<vscode.DocumentLink[]> {
        const links: vscode.DocumentLink[] = [];
        const text = document.getText();
        const stepIds = extractStepIds(text);

        const stepsPattern = /\$\{steps\.([a-zA-Z_][a-zA-Z0-9_]*)\.outputs\.([a-zA-Z_][a-zA-Z0-9_]*)\}/g;
        let match;
        while ((match = stepsPattern.exec(text)) !== null) {
            const stepId = match[1];
            if (stepIds.has(stepId)) {
                const startPos = document.positionAt(match.index);
                const endPos = document.positionAt(match.index + match[0].length);
                const range = new vscode.Range(startPos, endPos);
                
                const link = new vscode.DocumentLink(range);
                link.tooltip = `Go to step "${stepId}"`;
                link.target = this.createStepUri(stepId);
                links.push(link);
            }
        }

        const simpleVarPattern = /\$\{([a-zA-Z_][a-zA-Z0-9_]*)\}/g;
        while ((match = simpleVarPattern.exec(text)) !== null) {
            if (match[1].includes(':') || match[1].includes('.')) continue;
            
            const varName = match[1];
            const varDefPattern = new RegExp(`^\\s*${varName}:`, 'm');
            if (varDefPattern.test(text)) {
                const startPos = document.positionAt(match.index);
                const endPos = document.positionAt(match.index + match[0].length);
                const range = new vscode.Range(startPos, endPos);
                
                const link = new vscode.DocumentLink(range);
                link.tooltip = `Go to variable "${varName}"`;
                link.target = this.createVariableUri(varName);
                links.push(link);
            }
        }

        return links;
    }

    resolveDocumentLink(link: vscode.DocumentLink, token: vscode.CancellationToken): vscode.ProviderResult<vscode.DocumentLink> {
        const data = this.parseUri(link.target);
        if (!data) return link;

        const document = vscode.window.activeTextEditor?.document;
        if (!document) return link;

        const text = document.getText();
        const lines = text.split('\n');

        if (data.type === 'step') {
            for (let i = 0; i < lines.length; i++) {
                const idMatch = lines[i].match(new RegExp(`^\\s*id:\\s*${data.name}\\s*$`));
                if (idMatch) {
                    link.target = document.uri.with({ fragment: `L${i + 1}` });
                    return link;
                }
            }
        } else if (data.type === 'variable') {
            for (let i = 0; i < lines.length; i++) {
                const varMatch = lines[i].match(new RegExp(`^\\s*${data.name}:`));
                if (varMatch) {
                    link.target = document.uri.with({ fragment: `L${i + 1}` });
                    return link;
                }
            }
        }

        return link;
    }

    private createStepUri(stepId: string): vscode.Uri {
        return vscode.Uri.parse(`autoflow://step/${stepId}`);
    }

    private createVariableUri(varName: string): vscode.Uri {
        return vscode.Uri.parse(`autoflow://variable/${varName}`);
    }

    private parseUri(uri: vscode.Uri): { type: 'step' | 'variable'; name: string } | null {
        if (uri.scheme !== 'autoflow') return null;
        const parts = uri.path.split('/').filter(Boolean);
        if (parts.length !== 2) return null;
        return { type: parts[0] as 'step' | 'variable', name: parts[1] };
    }
}
